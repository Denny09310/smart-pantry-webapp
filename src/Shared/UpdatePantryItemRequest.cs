using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public record UpdatePantryItemRequest(
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
