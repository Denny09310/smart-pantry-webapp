namespace Server.Data.Entities;

public class CosmeticItem
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Name { get; set; } = default!;

    public double Quantity { get; set; }
    public string Unit { get; set; } = default!;

    public string Category { get; set; } = default!;
    public string? Notes { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public string? HouseholdMemberId { get; set; }
    public HouseholdMember? HouseholdMember { get; set; }

    public string? Barcode { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
