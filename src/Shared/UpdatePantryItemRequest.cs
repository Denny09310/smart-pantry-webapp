using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

using Shared.Resources;

namespace Shared.Models;

[ValidatableType]
public record UpdatePantryItemRequest(
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_ItemNameRequired))]
    [StringLength(100, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_ItemNameLength))]
    string Name,
    [Range(0.01, 9999, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_QuantityRange))]
    double Quantity,
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_UnitRequired))]
    [StringLength(30, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_UnitLength))]
    string Unit,
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_LocationRequired))]
    [StringLength(60, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_LocationLength))]
    string Location,
    [StringLength(500, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_NotesLength))]
    string? Notes,
    DateOnly ExpirationDate) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => PantryItemRules.Validate(
            Name,
            Unit,
            Location,
            ExpirationDate,
            validationContext.GetService(typeof(IStringLocalizer<UIStrings>)) as IStringLocalizer<UIStrings>);
}
