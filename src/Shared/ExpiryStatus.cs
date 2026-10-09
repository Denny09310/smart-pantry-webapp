namespace Shared.Models;

public enum ExpiryStatus
{
    Expired,
    Today,
    Soon,
    Fresh,
}

/// <summary>
/// The single expiry rule (roadmap Phase 6). Pure and UI-independent:
/// used by the server when presenting items and by the client for previews.
/// </summary>
public static class ExpiryStatusCalculator
{
    public static ExpiryStatus GetStatus(DateOnly expirationDate, DateOnly today)
    {
        var days = expirationDate.DayNumber - today.DayNumber;
        return days switch
        {
            < 0 => ExpiryStatus.Expired,
            0 => ExpiryStatus.Today,
            <= 7 => ExpiryStatus.Soon,
            _ => ExpiryStatus.Fresh,
        };
    }
}
