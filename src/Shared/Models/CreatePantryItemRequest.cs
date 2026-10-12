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
    DateOnly ExpirationDate);