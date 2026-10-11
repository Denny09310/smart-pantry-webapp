namespace Shared.Models;

public record RecipeSuggestionDto(
    string IdMeal,
    string Name,
    string ThumbnailUrl,
    string? Area,
    string SourceUrl,
    IReadOnlyList<string> MatchedIngredients);

public record GetRecipeSuggestionsResponse(IReadOnlyList<RecipeSuggestionDto> Items);
