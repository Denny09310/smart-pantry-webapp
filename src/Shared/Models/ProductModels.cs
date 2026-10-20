using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

[ValidatableType]
public record LookupProductRequest(
    [property: Required]
    [property: RegularExpression(@"^\d{8}$|^\d{12}$|^\d{13}$", ErrorMessage = "Invalid barcode format. Must be 8, 12, or 13 digits.")]
    string? Barcode);

public record ProductLookupDto(
    string Barcode,
    string Name,
    string? Brand,
    double? Quantity,
    string? Unit,
    string? ImageUrl,
    string? IngredientsText = null);
