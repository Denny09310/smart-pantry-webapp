using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

[ValidatableType]
public record GetNotificationsRequest
{
    // Nullable so [AsParameters] treats missing query values as absent;
    // the endpoint applies defaults when null. [Range] still runs when set.
    [Range(0, int.MaxValue)]
    public int? Skip { get; set; }

    [Range(1, 100)]
    public int? Take { get; set; }
}