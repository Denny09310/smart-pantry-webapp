using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

[ValidatableType]
public record GetCosmeticItemsRequest
{
    // Nullable so [AsParameters] treats missing query values as absent;
    // the endpoint applies Defaults when null. [Range] still runs when set.
    [Range(0, int.MaxValue)]
    public int? Skip { get; set; }

    [Range(1, 100)]
    public int? Take { get; set; }

    public string? Category { get; set; }
    public string? Name { get; set; }

    /// <summary>Status filter (<c>expired</c>, <c>soon</c>, <c>good</c>); null means all.</summary>
    public string? Status { get; set; }

    /// <summary>
    /// Sort (<c>expiry-asc</c>, <c>expiry-desc</c>, <c>name</c>);
    /// defaults to <c>expiry-asc</c> in the handler.
    /// </summary>
    public string? Sort { get; set; }
}
