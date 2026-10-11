using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

using Shared.Resources;

namespace Shared.Models;

[ValidatableType]
public record UnsubscribePushRequest(
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_EndpointRequired))]
    string? Endpoint) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<UIStrings>)) as IStringLocalizer<UIStrings>;
        string Text(string key) => localizer?.GetString(key) ?? UIStrings.Get(key);

        if (string.IsNullOrWhiteSpace(Endpoint))
            yield return new ValidationResult(Text(nameof(UIStrings.V_EndpointRequired)), [nameof(Endpoint)]);
    }
}