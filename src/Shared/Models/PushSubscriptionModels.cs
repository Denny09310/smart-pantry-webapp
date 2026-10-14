using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

[ValidatableType]
public record PushSubscriptionRequest(
    [property: Required, MaxLength(2000)] string Endpoint,
    [property: Required, MaxLength(512)] string P256dh,
    [property: Required, MaxLength(512)] string Auth);

public record PushSubscriptionDto(
    string Id,
    string Endpoint,
    DateTimeOffset CreatedAt);