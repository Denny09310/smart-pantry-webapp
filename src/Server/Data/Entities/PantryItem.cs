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

    public string? HouseholdMemberId { get; set; }
    public HouseholdMember? HouseholdMember { get; set; }

    public string? Barcode { get; set; }

    public string? NutriScore { get; set; }

    public string? NutritionJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}