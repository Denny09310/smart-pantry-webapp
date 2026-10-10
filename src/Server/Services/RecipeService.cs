using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Caching.Memory;

namespace Server.Services;

/// <summary>
/// TheMealDB recipe suggestions for expiring stock: filter per ingredient,
/// look up the top candidates, rank by expiring-ingredient coverage.
/// Shared recipe data is cached in memory for 24h; failures yield an empty
/// list so the UI section simply hides.
/// </summary>
internal sealed class RecipeService(
    IHttpClientFactory http,
    IMemoryCache cache,
    ILogger<RecipeService> log)
{
    private const int MaxIngredients = 6;
    private const int MaxCandidates = 8;
    private const int MaxResults = 5;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<RecipeSuggestion>> GetSuggestionsAsync(
        IReadOnlyList<string> expiringNames,
        CancellationToken ct = default)
    {
        var queries = expiringNames
            .Select(NormalizeQuery)
            .Where(q => q.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaxIngredients)
            .ToList();

        if (queries.Count == 0)
            return [];

        var cacheKey = $"recipes:{string.Join('|', queries.OrderBy(q => q, StringComparer.Ordinal))}";

        if (cache.TryGetValue(cacheKey, out IReadOnlyList<RecipeSuggestion>? cached) && cached is not null)
            return cached;

        IReadOnlyList<RecipeSuggestion> result = [];

        try
        {
            result = await FetchSuggestionsAsync(queries, expiringNames, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Recipe suggestions failed.");
        }

        cache.Set(cacheKey, result, CacheLifetime);

        return result;
    }

    private async Task<IReadOnlyList<RecipeSuggestion>> FetchSuggestionsAsync(
        List<string> queries,
        IReadOnlyList<string> expiringNames,
        CancellationToken ct)
    {
        var client = http.CreateClient("TheMealDb");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var candidates = new List<(string Id, string Query)>();

        foreach (var query in queries)
        {
            ct.ThrowIfCancellationRequested();

            List<MealSummary> meals;

            try
            {
                meals = await FilterByIngredientAsync(client, query, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "Recipe filter for {Ingredient} failed.", query);
                continue;
            }

            foreach (var meal in meals)
            {
                if (candidates.Count >= MaxCandidates)
                    break;

                if (seen.Add(meal.IdMeal))
                    candidates.Add((meal.IdMeal, query));
            }

            if (candidates.Count >= MaxCandidates)
                break;
        }

        var lookups = await Task.WhenAll(candidates.Select(c =>
            LookupDetailsAsync(client, c.Id, ct)));

        var expiring = expiringNames
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return lookups
            .Where(m => m is not null)
            .Select(m =>
            {
                var matched = expiring
                    .Where(e => m!.Ingredients.Any(i => Covers(i, e)))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return (Meal: m!, Matched: matched);
            })
            .Where(x => x.Matched.Count > 0)
            .OrderByDescending(x => x.Matched.Count)
            .ThenBy(x => x.Meal.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaxResults)
            .Select(x => new RecipeSuggestion(
                x.Meal.IdMeal,
                x.Meal.Name,
                $"{x.Meal.Thumbnail}/small",
                x.Meal.Area,
                $"https://www.themealdb.com/meal/{x.Meal.IdMeal}",
                x.Matched))
            .ToList();
    }

    private static bool Covers(string mealIngredient, string expiring)
        => mealIngredient.Contains(expiring, StringComparison.OrdinalIgnoreCase)
            || expiring.Contains(mealIngredient, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeQuery(string name)
        => name.Trim().ToLowerInvariant().Replace(' ', '_');

    private static async Task<List<MealSummary>> FilterByIngredientAsync(
        HttpClient client,
        string query,
        CancellationToken ct)
    {
        using var response = await client.GetAsync(
            $"filter.php?i={Uri.EscapeDataString(query)}", ct);

        if (!response.IsSuccessStatusCode)
            return [];

        var payload = await response.Content.ReadFromJsonAsync<MealFilterResponse>(ct);

        return payload?.Meals?
            .Where(m => m.IdMeal.Length > 0 && m.Name.Length > 0)
            .Select(m => new MealSummary(m.IdMeal, m.Name))
            .ToList() ?? [];
    }

    private async Task<MealDetails?> LookupDetailsAsync(HttpClient client, string id, CancellationToken ct)
    {
        try
        {
            using var response = await client.GetAsync($"lookup.php?i={Uri.EscapeDataString(id)}", ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync<MealLookupResponse>(ct);
            var meal = payload?.Meals?.FirstOrDefault();

            if (meal is null || meal.Name.Length == 0)
                return null;

            return new MealDetails(meal.IdMeal, meal.Name, meal.Thumbnail, meal.Area, meal.Ingredients());
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Recipe lookup for {MealId} failed.", id);
            return null;
        }
    }

    private sealed record MealSummary(string IdMeal, string Name);

    private sealed record MealFilterResponse(
        [property: JsonPropertyName("meals")] List<MealFilterMeal>? Meals);

    private sealed record MealFilterMeal(
        [property: JsonPropertyName("idMeal")] string IdMeal,
        [property: JsonPropertyName("strMeal")] string Name);

    private sealed record MealLookupResponse(
        [property: JsonPropertyName("meals")] List<MealLookupMeal>? Meals);

    private sealed record MealLookupMeal(
        [property: JsonPropertyName("idMeal")] string IdMeal,
        [property: JsonPropertyName("strMeal")] string Name,
        [property: JsonPropertyName("strMealThumb")] string Thumbnail,
        [property: JsonPropertyName("strArea")] string? Area)
    {
        // strIngredient1..20 arrive as flat keys; collect the filled slots.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }

        public IReadOnlyList<string> Ingredients()
        {
            if (Extra is null)
                return [];

            return Extra
                .Where(kv => kv.Key.StartsWith("strIngredient", StringComparison.Ordinal)
                    && kv.Value.ValueKind == JsonValueKind.String)
                .Select(kv => kv.Value.GetString()!.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }
    }

    private sealed record MealDetails(
        string IdMeal,
        string Name,
        string Thumbnail,
        string? Area,
        IReadOnlyList<string> Ingredients);
}

/// <summary>Ranked recipe ready for the response DTO.</summary>
internal sealed record RecipeSuggestion(
    string IdMeal,
    string Name,
    string ThumbnailUrl,
    string? Area,
    string SourceUrl,
    IReadOnlyList<string> MatchedIngredients);
