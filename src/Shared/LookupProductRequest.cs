using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

using Shared.Resources;

namespace Shared.Models;

[ValidatableType]
public record LookupProductRequest(
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_BarcodeRequired))]
    [RegularExpression(@"^\d{8}$|^\d{12}$|^\d{13}$", ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_BarcodeFormat))]
    string? Barcode);
