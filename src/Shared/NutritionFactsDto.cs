namespace Shared.Models;

/// <summary>
/// Per-100-g nutrition facts from Open Food Facts. Every value is optional:
/// OFF omits whatever the label does not declare.
/// </summary>
public record NutritionFactsDto(
    double? EnergyKcal100g,
    double? Fat100g,
    double? SaturatedFat100g,
    double? Carbohydrates100g,
    double? Sugars100g,
    double? Fiber100g,
    double? Proteins100g,
    double? Salt100g);
