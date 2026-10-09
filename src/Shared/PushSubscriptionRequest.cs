using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public record PushSubscriptionRequest(
    [Required]
    string Endpoint,

    [Required]
    string P256dh,

    [Required]
    string Auth);
