using System.ComponentModel.DataAnnotations;

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
    [RegularExpression("^(en|it)$", ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_LanguageSupported))]
    string? Language = null);
