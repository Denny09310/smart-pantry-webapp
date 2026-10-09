using System.Globalization;

using BlazorBlueprint.Components;

using Shared.Models;

namespace Client.Models;

public enum PantryItemStatus
{
    Expired,
    Today,
    Soon,
    Fresh,
}

public enum PantryStatusFilter
{
    All,
    Soon,
    Today,
    Expired,
    Fresh,
}

public static class PantryItemStatusExtensions
{
    public static PantryItemStatus GetStatus(this PantryItemDto item)
        => item.Status switch
        {
            ExpiryStatus.Expired => PantryItemStatus.Expired,
            ExpiryStatus.Today => PantryItemStatus.Today,
            ExpiryStatus.Soon => PantryItemStatus.Soon,
            _ => PantryItemStatus.Fresh,
        };

    public static PantryItemStatus GetStatus(DateOnly expirationDate)
        => ExpiryStatusCalculator
            .GetStatus(expirationDate, DateOnly.FromDateTime(DateTime.Today)) switch
        {
            ExpiryStatus.Expired => PantryItemStatus.Expired,
            ExpiryStatus.Today => PantryItemStatus.Today,
            ExpiryStatus.Soon => PantryItemStatus.Soon,
            _ => PantryItemStatus.Fresh,
        };

    public static int CountByStatus(this IEnumerable<PantryItemDto> items, PantryItemStatus status)
        => items.Count(i => i.GetStatus() == status);

    public static IReadOnlyList<string> DistinctLocations(this IEnumerable<PantryItemDto> items)
        => items
            .Select(i => i.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyDictionary<string, int> CountByLocation(this IEnumerable<PantryItemDto> items)
        => items
            .GroupBy(i => i.Location, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
    public static int DaysRemaining(this PantryItemDto item)
        => item.ExpirationDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    public static int UrgencyRank(this PantryItemDto item)
        => item.GetStatus() switch
        {
            PantryItemStatus.Expired => 0,
            PantryItemStatus.Today => 1,
            PantryItemStatus.Soon => 2,
            _ => 3,
        };

    public static bool Matches(this PantryItemDto item, PantryStatusFilter filter)
        => filter switch
        {
            PantryStatusFilter.Soon => item.GetStatus() is PantryItemStatus.Soon,
            PantryStatusFilter.Today => item.GetStatus() is PantryItemStatus.Today,
            PantryStatusFilter.Expired => item.GetStatus() is PantryItemStatus.Expired,
            PantryStatusFilter.Fresh => item.GetStatus() is PantryItemStatus.Fresh,
            _ => true,
        };

    public static string GetLabel(this PantryItemDto item)
        => item.GetStatus() switch
        {
            PantryItemStatus.Expired => "Expired",
            PantryItemStatus.Today => "Expires today",
            PantryItemStatus.Soon => $"Expires in {item.DaysRemaining()} days",
            _ => "Fresh",
        };

    public static BadgeVariant GetBadgeVariant(this PantryItemDto item)
        => item.GetStatus() switch
        {
            PantryItemStatus.Expired => BadgeVariant.SoftDestructive,
            PantryItemStatus.Today => BadgeVariant.SoftWarning,
            PantryItemStatus.Soon => BadgeVariant.SoftInfo,
            _ => BadgeVariant.SoftSuccess,
        };

    public static string GetDisplayLocation(this PantryItemDto item)
        => string.IsNullOrEmpty(item.Location)
            ? item.Location
            : char.ToUpperInvariant(item.Location[0]) + item.Location[1..];

    public static string FormatExpiration(this PantryItemDto item)
        => item.GetStatus() is PantryItemStatus.Today
            ? "Today"
            : item.ExpirationDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string FormatQuantity(this PantryItemDto item)
    {
        var quantity = item.Quantity % 1 == 0
            ? ((int)item.Quantity).ToString(CultureInfo.InvariantCulture)
            : item.Quantity.ToString("0.##", CultureInfo.InvariantCulture);

        return $"{quantity} {item.Unit}";
    }

    public static string GetInitials(this PantryItemDto item)
    {
        var initials = string.Concat(item.Name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(w => w[0]));

        return string.IsNullOrEmpty(initials) ? "?" : initials.ToUpperInvariant();
    }
}