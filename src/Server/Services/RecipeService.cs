using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace Server.Services;

public interface IRecipeService
{
    /// <summary>Meals containing every given ingredient (English names).</summary>
    Task<IReadOnlyList<ThemealDbMeal>> SearchByIngredientsAsync(
        IReadOnlyList<string> ingredients, CancellationToken ct);

    Task<ThemealDbMealDetail?> GetMealAsync(string id, CancellationToken ct);

    /// <summary>All known ingredient names, cached in memory.</summary>
    Task<IReadOnlyList<string>> GetIngredientNamesAsync(CancellationToken ct);
}

public sealed record ThemealDbMeal(string Id, string Name, string? Thumbnail, string? Area);

public sealed record ThemealDbIngredient(string Name, string? Measure);

public sealed record ThemealDbMealDetail(
    string Id,
    string Name,
    string? Area,
    string? Category,
    string? Instructions,
    string? Thumbnail,
    IReadOnlyList<ThemealDbIngredient> Ingredients);

/// <summary>TheMealDB: free, keyless recipe search (English-only data).</summary>
public sealed class ThemealDbRecipeService(HttpClient http, IMemoryCache cache) : IRecipeService
{
    private const string BaseUrl = "https://www.themealdb.com/api/json/v1/1/";
    private const string IngredientListKey = "themealdb:ingredients";

    public async Task<IReadOnlyList<ThemealDbMeal>> SearchByIngredientsAsync(
        IReadOnlyList<string> ingredients, CancellationToken ct)
    {
        var distinct = ingredients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinct.Count == 0)
            return [];

        // One-ingredient filters only: fan out in parallel, intersect by meal.
        var perIngredient = await Task.WhenAll(
            distinct.Select(name => FilterByIngredientAsync(name, ct)));

        var intersection = perIngredient[0].ToDictionary(x => x.Id);
        foreach (var list in perIngredient.Skip(1))
            intersection = list.Where(x => intersection.ContainsKey(x.Id))
                .ToDictionary(x => x.Id);

        return intersection.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ThemealDbMealDetail?> GetMealAsync(string id, CancellationToken ct)
    {
        var response = await GetAsync<MealLookupResponse>($"lookup.php?i={Uri.EscapeDataString(id)}", ct);
        var meal = response?.Meals?.FirstOrDefault();
        if (meal is null)
            return null;

        var ingredients = new List<ThemealDbIngredient>();
        for (var i = 1; i <= 20; i++)
        {
            var name = meal.GetIngredient(i);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            ingredients.Add(new ThemealDbIngredient(name.Trim(), meal.GetMeasure(i)?.Trim()));
        }

        return new ThemealDbMealDetail(
            meal.IdMeal ?? id,
            meal.Name ?? "Unknown meal",
            meal.Area,
            meal.Category,
            meal.Instructions,
            meal.Thumbnail,
            ingredients);
    }

    public async Task<IReadOnlyList<string>> GetIngredientNamesAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(IngredientListKey, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        var response = await GetAsync<IngredientListResponse>("list.php?i=list", ct);
        IReadOnlyList<string> names = response?.Meals?
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        cache.Set(IngredientListKey, names, TimeSpan.FromHours(24));
        return names;
    }

    private async Task<IReadOnlyList<ThemealDbMeal>> FilterByIngredientAsync(string ingredient, CancellationToken ct)
    {
        var response = await GetAsync<MealFilterResponse>(
            $"filter.php?i={Uri.EscapeDataString(ingredient)}", ct);

        return response?.Meals?
            .Where(x => x.IdMeal is not null && x.Name is not null)
            .Select(x => new ThemealDbMeal(x.IdMeal!, x.Name!, x.Thumbnail, x.Area))
            .ToList() ?? [];
    }

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct)
    {
        try
        {
            return await http.GetFromJsonAsync<T>(
                new Uri(new Uri(BaseUrl), relativeUrl), ct);
        }
        catch (HttpRequestException)
        {
            return default;
        }
        catch (JsonException)
        {
            return default;
        }
        catch (OperationCanceledException)
        {
            return default;
        }
    }

    private sealed class MealFilterResponse
    {
        [JsonPropertyName("meals")]
        public List<MealSummary>? Meals { get; set; }
    }

    private sealed class MealSummary
    {
        [JsonPropertyName("idMeal")]
        public string? IdMeal { get; set; }

        [JsonPropertyName("strMeal")]
        public string? Name { get; set; }

        [JsonPropertyName("strMealThumb")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("strArea")]
        public string? Area { get; set; }
    }

    private sealed class MealLookupResponse
    {
        [JsonPropertyName("meals")]
        public List<MealDetail>? Meals { get; set; }
    }

    private sealed class MealDetail
    {
        [JsonPropertyName("idMeal")]
        public string? IdMeal { get; set; }

        [JsonPropertyName("strMeal")]
        public string? Name { get; set; }

        [JsonPropertyName("strArea")]
        public string? Area { get; set; }

        [JsonPropertyName("strCategory")]
        public string? Category { get; set; }

        [JsonPropertyName("strInstructions")]
        public string? Instructions { get; set; }

        [JsonPropertyName("strMealThumb")]
        public string? Thumbnail { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

        public string? GetIngredient(int index) => GetExtra($"strIngredient{index}");
        public string? GetMeasure(int index) => GetExtra($"strMeasure{index}");

        private string? GetExtra(string key)
        {
            if (Extra is null || !Extra.TryGetValue(key, out var element))
                return null;
            return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        }
    }

    private sealed class IngredientListResponse
    {
        [JsonPropertyName("meals")]
        public List<IngredientEntry>? Meals { get; set; }
    }

    private sealed class IngredientEntry
    {
        [JsonPropertyName("strIngredient")]
        public string? Name { get; set; }
    }
}
