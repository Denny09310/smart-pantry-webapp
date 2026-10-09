using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record CreatePantryItemRequest(
    [Required]
    [StringLength(100)]
    string Name,

    [Range(1, 9999)]
    double Quantity,

    [Required]
    string Unit,

    [Required]
    string Location,

    [StringLength(500)]
    string? Notes,

    DateOnly ExpirationDate);