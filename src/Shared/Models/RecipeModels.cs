using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

/// <summary>Recipe search by ingredient tags, e.g. <c>?ingredients=tomato,cheese&amp;lang=it</c>.</summary>
[ValidatableType]
public record SearchRecipesRequest(
    string? Ingredients,
    string? Lang);

/// <summary>A recipe suggestion: thumbnail list entry.</summary>
public record RecipeSummaryDto(
    string Id,
    string Name,
    string? TranslatedName,
    string? Thumbnail,
    string? Area);

public record SearchRecipesResponse(IReadOnlyList<RecipeSummaryDto> Meals);

/// <summary>Full recipe detail with ingredient/measure pairs.</summary>
public record RecipeIngredientDto(string Name, string? Measure);

public record RecipeDetailDto(
    string Id,
    string Name,
    string? TranslatedName,
    string? Area,
    string? Category,
    string? Instructions,
    string? Thumbnail,
    IReadOnlyList<RecipeIngredientDto> Ingredients);

/// <summary>Ingredient autocomplete suggestions for the tag input.</summary>
public record IngredientSuggestionsResponse(IReadOnlyList<string> Ingredients);

/// <summary>Recipe detail lookup, e.g. <c>/api/recipes/52772?lang=it</c>.</summary>
[ValidatableType]
public record RecipeDetailRequest(string? Lang);

/// <summary>Ingredient autocomplete, e.g. <c>/api/recipes/ingredients?q=tom&amp;lang=it</c>.</summary>
[ValidatableType]
public record SuggestIngredientsRequest(string? Q, string? Lang);
