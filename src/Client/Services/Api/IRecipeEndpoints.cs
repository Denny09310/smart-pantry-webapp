using Refit;
using Shared.Models;

namespace Client.Services.Api;

[Headers("Accept: application/json")]
public interface IRecipeEndpoints
{
    [Get("/api/recipes/search")]
    Task<ApiResponse<SearchRecipesResponse>> SearchAsync(
        [Query] string? ingredients,
        [Query] string? lang,
        CancellationToken ct = default);

    [Get("/api/recipes/{id}")]
    Task<ApiResponse<RecipeDetailDto>> GetAsync(
        string id,
        [Query] string? lang,
        CancellationToken ct = default);

    [Get("/api/recipes/ingredients")]
    Task<ApiResponse<IngredientSuggestionsResponse>> SuggestIngredientsAsync(
        [Query, AliasAs("q")] string query,
        [Query] string? lang,
        CancellationToken ct = default);
}
