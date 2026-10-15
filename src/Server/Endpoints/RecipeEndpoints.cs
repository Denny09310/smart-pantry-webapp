using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;

using Server.Services;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Recipes")]
[MapGroup("/api/recipes")]
internal class RecipeEndpoints(IRecipeService recipes, ITranslationService translations)
{
    private const int MaxIngredients = 5;

    [MapGet("/search")]
    public async Task<Ok<SearchRecipesResponse>> SearchAsync(
        [AsParameters] SearchRecipesRequest request,
        CancellationToken ct)
    {
        var tags = (request.Ingredients ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxIngredients)
            .ToList();

        if (tags.Count == 0)
            return TypedResults.Ok(new SearchRecipesResponse([]));

        var lang = (request.Lang ?? "en").ToLowerInvariant();

        // Tags already in the English ingredient list skip translation: saves
        // quota and avoids mangling words valid in both languages ("pizza").
        var known = await recipes.GetIngredientNamesAsync(ct);
        var knownSet = new HashSet<string>(known, StringComparer.OrdinalIgnoreCase);
        var englishTags = lang == "en"
            ? tags
            : await TranslateUnknownAsync(tags, knownSet, lang, ct);

        var meals = await recipes.SearchByIngredientsAsync(englishTags, ct);

        var summaries = lang == "en"
            ? meals.Select(x => new RecipeSummaryDto(x.Id, x.Name, null, x.Thumbnail, x.Area)).ToList()
            : await TranslateNamesAsync(meals, lang, ct);

        return TypedResults.Ok(new SearchRecipesResponse(summaries));
    }

    [MapGet("/{id}")]
    public async Task<Results<Ok<RecipeDetailDto>, NotFound, ValidationProblem>> GetAsync(
        string id,
        [AsParameters] RecipeDetailRequest request,
        CancellationToken ct)
    {
        var meal = await recipes.GetMealAsync(id, ct);
        if (meal is null)
            return TypedResults.NotFound();

        var lang = (request.Lang ?? "en").ToLowerInvariant();
        var translatedName = lang == "en"
            ? null
            : await translations.TranslateAsync(meal.Name, "en", lang, ct);

        return TypedResults.Ok(new RecipeDetailDto(
            meal.Id,
            meal.Name,
            translatedName == meal.Name ? null : translatedName,
            meal.Area,
            meal.Category,
            meal.Instructions,
            meal.Thumbnail,
            meal.Ingredients.Select(x => new RecipeIngredientDto(x.Name, x.Measure)).ToList()));
    }

    [MapGet("/ingredients")]
    public async Task<Ok<IngredientSuggestionsResponse>> SuggestIngredientsAsync(
        [AsParameters] SuggestIngredientsRequest request,
        CancellationToken ct)
    {
        var query = (request.Q ?? string.Empty).Trim();
        if (query.Length < 2)
            return TypedResults.Ok(new IngredientSuggestionsResponse([]));

        var lang = (request.Lang ?? "en").ToLowerInvariant();

        // Match the raw query first so English input (and identical words)
        // never spends translation quota; translate only on a miss.
        var names = await recipes.GetIngredientNamesAsync(ct);
        var matches = MatchIngredients(names, query);
        if (matches.Count == 0 && lang != "en")
        {
            var english = await translations.TranslateAsync(query, lang, "en", ct);
            if (!english.Equals(query, StringComparison.OrdinalIgnoreCase))
                matches = MatchIngredients(names, english);
        }

        return TypedResults.Ok(new IngredientSuggestionsResponse(matches));
    }

    private static List<string> MatchIngredients(IReadOnlyList<string> names, string query) =>
        names.Where(x => x.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

    private async Task<List<string>> TranslateUnknownAsync(
        List<string> tags, HashSet<string> known, string lang, CancellationToken ct)
    {
        var translated = await Task.WhenAll(tags.Select(t =>
            known.Contains(t)
                ? Task.FromResult(t)
                : translations.TranslateAsync(t, lang, "en", ct)));
        return translated.ToList();
    }

    /// <summary>Translation quota guard: only the head is translated eagerly.</summary>
    private const int MaxTranslatedNames = 20;

    private async Task<List<RecipeSummaryDto>> TranslateNamesAsync(
        IReadOnlyList<ThemealDbMeal> meals, string lang, CancellationToken ct)
    {
        var head = meals.Take(MaxTranslatedNames).ToList();
        var translated = await Task.WhenAll(
            head.Select(m => translations.TranslateAsync(m.Name, "en", lang, ct)));

        // The rest keep a null TranslatedName; clients fall back to Name.
        return head
            .Zip(translated, (meal, name) => new RecipeSummaryDto(
                meal.Id,
                meal.Name,
                name == meal.Name ? null : name,
                meal.Thumbnail,
                meal.Area))
            .Concat(meals.Skip(MaxTranslatedNames).Select(m => new RecipeSummaryDto(
                m.Id,
                m.Name,
                null,
                m.Thumbnail,
                m.Area)))
            .ToList();
    }
}