namespace Server.Data.Entities;

public class PantryItem
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Name { get; set; } = default!;

    public double Quantity { get; set; }
    public string Unit { get; set; } = default!;

    public string Location { get; set; } = default!;
    public string? Notes { get; set; }

    public DateOnly ExpirationDate { get; set; }

    /// <summary>
    /// Attribution only ("added by…"): never used for access checks.
    /// Null when the member was deleted or the writer sent none.
    /// </summary>
    public string? CreatedByMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}