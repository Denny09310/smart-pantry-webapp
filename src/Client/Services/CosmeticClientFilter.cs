using Client.Models;
using Shared.Models;

namespace Client.Services;

/// <summary>
/// Client-side mirror of the server cosmetics query (filters + ordering) so
/// realtime broadcasts can patch loaded rows without a full refetch.
/// Approximate by design: any doubt falls back to a full reload, which
/// self-corrects placement and membership.
/// </summary>
public static class CosmeticClientFilter
{
    /// <summary>Mirrors the server query: exact location, substring name, derived status.</summary>
    public static bool MatchesFilters(
        CosmeticItemDto item, string? location, string? name, string status, DateOnly today)
    {
        if (!string.IsNullOrWhiteSpace(location)
            && location != CosmeticCategories.All
            && !string.Equals(item.Category, location, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(name)
            && !item.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            return false;

        if (status != CosmeticRules.StatusAll)
        {
            var itemStatus = ExpiryHelper.GetStatus(item.ExpiryDate, today);
            var matches = status switch
            {
                var s when s == CosmeticRules.StatusExpired => itemStatus == ExpiryStatus.Expired,
                var s when s == CosmeticRules.StatusSoon => itemStatus == ExpiryStatus.ExpiringSoon,
                var s when s == CosmeticRules.StatusGood => itemStatus == ExpiryStatus.Good,
                _ => true
            };
            if (!matches)
                return false;
        }

        return true;
    }

    /// <summary>Mirrors the server ordering; any drift self-corrects on the next full reload.</summary>
    public static Comparison<CosmeticItemDto> GetComparer(ItemSort sort) => sort switch
    {
        ItemSort.ExpiryDesc => (a, b) =>
        {
            var cmp = b.ExpiryDate.CompareTo(a.ExpiryDate);
            return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        },
        ItemSort.Name => (a, b) =>
        {
            var cmp = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        },
        _ => (a, b) =>
        {
            var cmp = a.ExpiryDate.CompareTo(b.ExpiryDate);
            return cmp != 0 ? cmp : string.Compare(a.Id, b.Id, StringComparison.Ordinal);
        }
    };
}
