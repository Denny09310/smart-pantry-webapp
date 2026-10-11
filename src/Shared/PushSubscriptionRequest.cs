using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

using Shared.Resources;

namespace Shared.Models;

[ValidatableType]
public record PushSubscriptionRequest(
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_EndpointRequired))]
    [StringLength(2000, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_EndpointLength))]
    string Endpoint,
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_P256dhRequired))]
    [StringLength(500, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_P256dhLength))]
    string P256dh,
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_AuthRequired))]
    [StringLength(500, ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_AuthLength))]
    string Auth,
    [RegularExpression("^(?i:(en|it))$", ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_LanguageSupported))]
    string? Language = null) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var localizer = validationContext.GetService(typeof(IStringLocalizer<UIStrings>)) as IStringLocalizer<UIStrings>;
        string Text(string key) => localizer?.GetString(key) ?? UIStrings.Get(key);

        if (string.IsNullOrWhiteSpace(Endpoint))
            yield return new ValidationResult(Text(nameof(UIStrings.V_EndpointRequired)), [nameof(Endpoint)]);

        if (string.IsNullOrWhiteSpace(P256dh))
            yield return new ValidationResult(Text(nameof(UIStrings.V_P256dhRequired)), [nameof(P256dh)]);

        if (string.IsNullOrWhiteSpace(Auth))
            yield return new ValidationResult(Text(nameof(UIStrings.V_AuthRequired)), [nameof(Auth)]);
    }
}