using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record LookupProductRequest(
    [Required(ErrorMessage = "A barcode query value is required.")]
    [RegularExpression(@"^\d{8}$|^\d{12}$|^\d{13}$", ErrorMessage = "Barcode must be 8, 12 or 13 digits.")]
    string? Barcode);
