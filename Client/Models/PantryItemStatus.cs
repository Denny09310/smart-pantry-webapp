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
}

public static class PantryItemStatusExtensions
{
    public static PantryItemStatus GetStatus(this PantryItemDto item)
        => GetStatus(item.ExpirationDate);

    public static PantryItemStatus GetStatus(DateOnly expirationDate)
    {
        var days = expirationDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;
        return days switch
        {
            < 0 => PantryItemStatus.Expired,
            0 => PantryItemStatus.Today,
            <= 7 => PantryItemStatus.Soon,
            _ => PantryItemStatus.Fresh,
        };
    }

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
