using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

[ValidatableType]
public record CreatePantryItemRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Range(0, double.MaxValue)] double Quantity,
    [property: Required, MaxLength(32)] string Unit,
    [property: Required, MaxLength(64)] string Location,
    [property: MaxLength(2000)] string? Notes,
    DateOnly ExpirationDate,
    [property: MaxLength(32)] string? Barcode = null,
    [property: MaxLength(2)] string? NutriScore = null,
    NutritionFacts? Nutrition = null,
    // Optional client-provided id (GUID) for idempotent offline replay.
    // When omitted the server generates one.
    [property: RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        ErrorMessage = "Invalid id format. Must be a GUID.")]
    string? Id = null);