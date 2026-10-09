using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record CreatePantryItemRequest(
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, ErrorMessage = "Name must be 100 characters or fewer.")]
    string Name,
    [Range(0.01, 9999, ErrorMessage = "Quantity must be between 0.01 and 9999.")]
    double Quantity,
    [Required(ErrorMessage = "Unit is required.")]
    [StringLength(30, ErrorMessage = "Unit must be 30 characters or fewer.")]
    string Unit,
    [Required(ErrorMessage = "Location is required.")]
    [StringLength(60, ErrorMessage = "Location must be 60 characters or fewer.")]
    string Location,
    [StringLength(500, ErrorMessage = "Notes must be 500 characters or fewer.")]
    string? Notes,
    DateOnly ExpirationDate) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => PantryItemRules.Validate(Name, Unit, Location, ExpirationDate);
}
