using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

using Shared.Resources;

namespace Shared.Models;

[ValidatableType]
public record CreateMemberRequest(
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_MemberNameRequired))]
    [StringLength(30, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_MemberNameLength))]
    string Name,
    [StringLength(30, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_MemberColorLength))]
    string? Color) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<UIStrings>)) as IStringLocalizer<UIStrings>;

        return string.IsNullOrWhiteSpace(Name)
            ? [new ValidationResult(localizer?.GetString(nameof(UIStrings.V_MemberNameRequired)) ?? UIStrings.Get(nameof(UIStrings.V_MemberNameRequired)), ["Name"])]
            : [];
    }
}
