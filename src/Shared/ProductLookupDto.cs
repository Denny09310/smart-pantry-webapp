namespace Shared.Models;

public record ProductLookupDto(
    string Barcode,
    string Name,
    string? Brand,
    string? Quantity,
    string? ImageUrl,
    string? IngredientsText = null,
    string? NutriScoreGrade = null,
    NutritionFactsDto? Nutrition = null);
