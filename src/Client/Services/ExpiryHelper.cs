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
    public const int SoonDays = 7;

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
}
