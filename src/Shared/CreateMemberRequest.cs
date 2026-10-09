using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record CreateMemberRequest(
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(30, ErrorMessage = "Name must be 30 characters or fewer.")]
    string Name,
    [StringLength(30, ErrorMessage = "Color must be 30 characters or fewer.")]
    string? Color) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => string.IsNullOrWhiteSpace(Name)
            ? [new ValidationResult("Name is required.", ["Name"])]
            : [];
}
