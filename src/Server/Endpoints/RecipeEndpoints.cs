using Microsoft.AspNetCore.Generated.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Services;

using Shared.Models;

namespace Server.Endpoints;

[Tags("Recipes")]
[MapGroup("/api/recipes")]
internal class RecipeEndpoints(ApplicationDbContext db, RecipeService recipes)
{
    [MapGet("/suggestions")]
    public async Task<Ok<GetRecipeSuggestionsResponse>> GetSuggestionsAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(7);

        var expiring = await db.Items.AsNoTracking()
            .Where(i => i.ExpirationDate <= horizon)
            .OrderBy(i => i.ExpirationDate)
            .Select(i => i.Name)
            .Distinct()
            .Take(20)
            .ToListAsync(ct);

        var suggestions = await recipes.GetSuggestionsAsync(expiring, ct);

        return TypedResults.Ok(new GetRecipeSuggestionsResponse(
            suggestions.Select(s => new RecipeSuggestionDto(
                s.IdMeal,
                s.Name,
                s.ThumbnailUrl,
                s.Area,
                s.SourceUrl,
                s.MatchedIngredients))
            .ToList()));
    }
}
