using BlazorBlueprint.Components;

namespace Client.Services;

/// <summary>
/// Minimal expiry derivation for Phase 3 (list badges, dashboard counts).
/// Phase 4 systematizes status, sorting and filtering — keep all rules here
/// so there is a single place to extend.
/// </summary>
public enum ExpiryStatus
{
    Expired,
    ExpiringSoon,
    Good
}

public static class ExpiryHelper
{
    /// <summary>Days ahead that still counts as "expiring soon" (roadmap reminder window).</summary>
    public const int SoonDays = Shared.Models.CosmeticRules.ExpirySoonDays;

    public static ExpiryStatus GetStatus(DateOnly expiration, DateOnly today)
        => expiration < today ? ExpiryStatus.Expired
            : expiration <= today.AddDays(SoonDays) ? ExpiryStatus.ExpiringSoon
            : ExpiryStatus.Good;

    public static string Label(ExpiryStatus status, LocalizationService loc) => status switch
    {
        ExpiryStatus.Expired => loc["StatusExpired"],
        ExpiryStatus.ExpiringSoon => loc["StatusExpiringSoon"],
        _ => loc["StatusGood"]
    };

    public static BadgeVariant Badge(ExpiryStatus status) => status switch
    {
        ExpiryStatus.Expired => BadgeVariant.Destructive,
        ExpiryStatus.ExpiringSoon => BadgeVariant.Warning,
        _ => BadgeVariant.SoftSuccess
    };

    /// <summary>Contextual banner for the editor.</summary>
    public sealed record ExpiryBanner(string Text, string Icon, BadgeVariant Variant);

    public static ExpiryBanner GetBanner(DateOnly expiration, DateOnly today, LocalizationService loc)
    {
        var days = expiration.DayNumber - today.DayNumber;

        if (days < 0)
            return new ExpiryBanner(
                days == -1 ? loc["ExpiredYesterday"] : loc["ExpiredDaysAgo", -days],
                "circle-alert",
                BadgeVariant.SoftDestructive);

        if (days == 0)
            return new ExpiryBanner(loc["ExpiresToday"], "info", BadgeVariant.SoftWarning);

        if (days <= SoonDays)
            return new ExpiryBanner(
                days == 1 ? loc["UseSoonOneDay"] : loc["UseSoonDaysLeft", days],
                "info",
                BadgeVariant.SoftWarning);

        // Note: days == 1 always lands in the "soon" branch above (SoonDays >= 1),
        // so there is no singular good case.
        return new ExpiryBanner(
            loc["GoodDaysLeft", days],
            "info",
            BadgeVariant.SoftSuccess);
    }
}