using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record UnsubscribePushRequest(
    [Required(ErrorMessage = "A subscription endpoint query value is required.")]
    string? Endpoint);
