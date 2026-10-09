using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record PushSubscriptionRequest(
    [Required]
    string Endpoint,

    [Required]
    string P256dh,

    [Required]
    string Auth);