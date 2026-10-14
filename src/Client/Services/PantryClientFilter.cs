using Client.Models;
using Shared.Models;

namespace Client.Services;

/// <summary>
/// Client-side mirror of the server pantry query (filters + ordering) so
/// realtime broadcasts can patch loaded rows without a full refetch.
/// Approximate by design: any doubt falls back to a full reload, which
/// self-corrects placement and membership.
/// </summary>
public static class PantryClientFilter
{
    /// <summary>Mirrors the server query: exact location, substring name, derived status.</summary>
    public static bool MatchesFilters(
        PantryItemDto item, string? location, string? name, string status, DateOnly today)
    {
        if (!string.IsNullOrWhiteSpace(location)
            && location != PantryLocations.All
            && !string.Equals(item.Location, location, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(name)
            && !item.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            return false;

        if (status != PantryRules.StatusAll)
        {
            var itemStatus = ExpiryHelper.GetStatus(item.ExpirationDate, today);
            var matches = status switch
            {
                var s when s == PantryRules.StatusExpired => itemStatus == ExpiryStatus.Expired,
                var s when s == PantryRules.StatusSoon => itemStatus == ExpiryStatus.ExpiringSoon,
                var s when s == PantryRules.StatusFresh => itemStatus == ExpiryStatus.Fresh,
                _ => true
            };
            if (!matches)
                return false;
        }

        return true;
    }

    /// <summary>Mirrors the server ordering; any drift self-corrects on the next full reload.</summary>
    public static Comparison<PantryItemDto> GetComparer(ItemSort sort) => sort switch
    {
        ItemSort.ExpiryDesc => (a, b) =>
        {
            var cmp = b.ExpirationDate.CompareTo(a.ExpirationDate);
            return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        },
        ItemSort.Name => (a, b) =>
        {
            var cmp = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        },
        _ => (a, b) =>
        {
            var cmp = a.ExpirationDate.CompareTo(b.ExpirationDate);
            return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        }
    };
}
