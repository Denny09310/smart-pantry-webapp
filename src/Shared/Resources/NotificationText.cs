using Microsoft.Extensions.Localization;

namespace Shared.Resources;

/// <summary>
/// Single source of truth for expiry sentences, used by the server (push
/// payloads, per-subscription language) and the client (notification list).
/// Singular branches keep both languages grammatical without plural rules.
/// </summary>
public static class NotificationText
{
    public static string Format(string itemName, DateOnly expirationDate, DateOnly today, IStringLocalizer localizer)
    {
        var days = expirationDate.DayNumber - today.DayNumber;

        return days switch
        {
            < 0 => -days == 1
                ? localizer["Notif_ExpiredYesterday", itemName]
                : localizer["Notif_ExpiredDaysAgo", itemName, -days],
            0 => localizer["Notif_ExpiresToday", itemName],
            1 => localizer["Notif_ExpiresTomorrow", itemName],
            _ => localizer["Notif_ExpiresInDays", itemName, days],
        };
    }
}
