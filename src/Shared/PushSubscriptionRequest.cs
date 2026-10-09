using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record PushSubscriptionRequest(
    [Required(ErrorMessage = "Endpoint is required.")]
    [StringLength(2000, ErrorMessage = "Endpoint must be 2000 characters or fewer.")]
    string Endpoint,
    [Required(ErrorMessage = "P256dh is required.")]
    [StringLength(500, ErrorMessage = "P256dh must be 500 characters or fewer.")]
    string P256dh,
    [Required(ErrorMessage = "Auth is required.")]
    [StringLength(500, ErrorMessage = "Auth must be 500 characters or fewer.")]
    string Auth);
