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
        var englishTags = lang == "en"
            ? tags
            : await TranslateAllAsync(tags, lang, "en", ct);

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

        // Match against the English ingredient list; translate the query first
        // so Italian users typing "pomod..." still get "Tomatoes".
        var lang = (request.Lang ?? "en").ToLowerInvariant();
        if (lang != "en")
            query = await translations.TranslateAsync(query, lang, "en", ct);

        var names = await recipes.GetIngredientNamesAsync(ct);
        var matches = names
            .Where(x => x.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToList();

        return TypedResults.Ok(new IngredientSuggestionsResponse(matches));
    }

    private async Task<List<string>> TranslateAllAsync(
        List<string> tags, string from, string to, CancellationToken ct) =>
        (await Task.WhenAll(tags.Select(t => translations.TranslateAsync(t, from, to, ct)))).ToList();

    private async Task<List<RecipeSummaryDto>> TranslateNamesAsync(
        IReadOnlyList<ThemealDbMeal> meals, string lang, CancellationToken ct)
    {
        var translated = await Task.WhenAll(
            meals.Select(m => translations.TranslateAsync(m.Name, "en", lang, ct)));

        return meals.Zip(translated, (meal, name) => new RecipeSummaryDto(
            meal.Id,
            meal.Name,
            name == meal.Name ? null : name,
            meal.Thumbnail,
            meal.Area)).ToList();
    }
}
