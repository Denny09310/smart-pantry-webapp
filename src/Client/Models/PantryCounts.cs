namespace Client.Models;

/// <summary>
/// Chip counts for the pantry toolbars. Each count is the TotalItems of a
/// single Take=1 paged request: no aggregate endpoint, just paging.
/// </summary>
public sealed record PantryCounts(
    int Total,
    int Expired,
    int Today,
    int Soon,
    int Fresh,
    int Attention,
    IReadOnlyDictionary<string, int> ByLocation);
