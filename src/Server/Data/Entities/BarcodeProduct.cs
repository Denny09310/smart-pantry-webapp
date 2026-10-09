namespace Server.Data.Entities;

/// <summary>
/// Local cache of Open Food Facts lookups: repeat scans resolve offline.
/// Keyed by barcode; rows are immutable once written (no refresh, no TTL).
/// </summary>
public class BarcodeProduct
{
    public string Barcode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Brand { get; set; }
    public string? Quantity { get; set; }
    public string? ImageUrl { get; set; }

    public DateTimeOffset LookedUpAt { get; set; } = DateTimeOffset.UtcNow;
}
