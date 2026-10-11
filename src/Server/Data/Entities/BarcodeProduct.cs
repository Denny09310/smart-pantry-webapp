namespace Server.Data.Entities;

/// <summary>
/// Local cache of Open Food Facts lookups: repeat scans resolve offline.
/// Keyed by barcode. Core fields are immutable once written; detail fields
/// (ingredients, nutrition) backfill on the next lookup of rows cached
/// before they existed.
/// </summary>
public class BarcodeProduct
{
    public string Barcode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public string? ImageUrl { get; set; }

    public string? IngredientsText { get; set; }
    public string? NutriScoreGrade { get; set; }
    public double? EnergyKcal100g { get; set; }
    public double? Fat100g { get; set; }
    public double? SaturatedFat100g { get; set; }
    public double? Carbohydrates100g { get; set; }
    public double? Sugars100g { get; set; }
    public double? Fiber100g { get; set; }
    public double? Proteins100g { get; set; }
    public double? Salt100g { get; set; }

    public DateTimeOffset LookedUpAt { get; set; } = DateTimeOffset.UtcNow;
}