using System.ComponentModel.DataAnnotations;
using Shared.Models;

namespace Client.Models;

public static class PantryLocations
{
    public const string All = "all";

    public static readonly string[] Values = ["fridge", "pantry", "freezer", "cabinet"];
}

public class ItemFormModel
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Quantity { get; set; } = 1;

    [Required, MaxLength(32)]
    public string Unit { get; set; } = "pcs";

    [Required, MaxLength(64)]
    public string Location { get; set; } = "pantry";

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Required]
    public DateOnly ExpirationDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

    public static ItemFormModel FromDto(PantryItemDto dto) => new()
    {
        Name = dto.Name,
        Quantity = dto.Quantity,
        Unit = dto.Unit,
        Location = dto.Location,
        Notes = dto.Notes,
        ExpirationDate = dto.ExpirationDate
    };

    public CreatePantryItemRequest ToCreateRequest() => new(
        Name, Quantity, Unit, Location, Notes, ExpirationDate);

    public UpdatePantryItemRequest ToUpdateRequest() => new(
        Name, Quantity, Unit, Location, Notes, ExpirationDate);
}
