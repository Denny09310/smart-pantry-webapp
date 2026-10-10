using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

using Shared.Resources;

namespace Shared.Models;

[ValidatableType]
public record UnsubscribePushRequest(
    [Required(ErrorMessageResourceType = typeof(UIStrings), ErrorMessageResourceName = nameof(UIStrings.V_EndpointRequired))]
    string? Endpoint);
