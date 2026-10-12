using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

[ValidatableType]
public record LookupProductRequest(
    [property: Required]
    [property: RegularExpression(@"^\d{8}$|^\d{12}$|^\d{13}$", ErrorMessage = "Invalid barcode format. Must be 8, 12, or 13 digits.")]
    string? Barcode);

/// <summary>Nutrition facts per 100g; every value optional.</summary>
public record NutritionFacts(
    double? EnergyKcal = null,
    double? Proteins = null,
    double? Carbohydrates = null,
    double? Sugars = null,
    double? Fat = null,
    double? SaturatedFat = null,
    double? Salt = null);

public static class NutritionFactsExtensions
{
    public static string ToJson(this NutritionFacts facts) =>
        JsonSerializer.Serialize(facts);

    public static NutritionFacts? Parse(string json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<NutritionFacts>(json);
}

public record ProductLookupDto(
    string Barcode,
    string Name,
    string? Brand,
    double? Quantity,
    string? Unit,
    string? NutriScore,
    NutritionFacts? Nutrition,
    string? ImageUrl);