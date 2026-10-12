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
    Fresh
}

public static class ExpiryHelper
{
    /// <summary>Days ahead that still counts as "expiring soon" (roadmap reminder window).</summary>
    public const int SoonDays = Shared.Models.PantryRules.ExpirySoonDays;

    public static ExpiryStatus GetStatus(DateOnly expiration, DateOnly today)
        => expiration < today ? ExpiryStatus.Expired
            : expiration <= today.AddDays(SoonDays) ? ExpiryStatus.ExpiringSoon
            : ExpiryStatus.Fresh;

    public static string Label(ExpiryStatus status) => status switch
    {
        ExpiryStatus.Expired => "Expired",
        ExpiryStatus.ExpiringSoon => "Expiring soon",
        _ => "Fresh"
    };

    public static BadgeVariant Badge(ExpiryStatus status) => status switch
    {
        ExpiryStatus.Expired => BadgeVariant.Destructive,
        ExpiryStatus.ExpiringSoon => BadgeVariant.Warning,
        _ => BadgeVariant.SoftSuccess
    };

    /// <summary>Contextual banner for the editor.</summary>
    public sealed record ExpiryBanner(string Text, string Icon, BadgeVariant Variant);

    public static ExpiryBanner GetBanner(DateOnly expiration, DateOnly today)
    {
        var days = expiration.DayNumber - today.DayNumber;

        if (days < 0)
            return new ExpiryBanner(
                days == -1 ? "Expired yesterday" : $"Expired {-days} days ago",
                "circle-alert",
                BadgeVariant.SoftDestructive);

        if (days == 0)
            return new ExpiryBanner("Expires today", "info", BadgeVariant.SoftWarning);

        if (days <= SoonDays)
            return new ExpiryBanner(
                days == 1 ? "Use soon · 1 day left" : $"Use soon · {days} days left",
                "info",
                BadgeVariant.SoftWarning);

        return new ExpiryBanner(
            days == 1 ? "Fresh · 1 day left" : $"Fresh · {days} days left",
            "info",
            BadgeVariant.SoftSuccess);
    }
}
