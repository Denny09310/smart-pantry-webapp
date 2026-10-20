namespace Server.Data.Entities;

/// <summary>
/// A successful OpenBeautyFacts result, cached by barcode so repeat scans
/// don't hit the network. Best-effort: a failed write never fails a lookup.
/// </summary>
public class CachedProduct
{
    public string Barcode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Brand { get; set; }
    public double? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? ImageUrl { get; set; }
    public string? IngredientsText { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
