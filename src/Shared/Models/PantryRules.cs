namespace Shared.Models;

/// <summary>
/// Shared pantry rules used by both tiers so client and server never drift:
/// the expiry-soon window, page limits, and the status/sort query literals.
/// </summary>
public static class PantryRules
{
    public const int ExpirySoonDays = 7;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public const string LocationAll = "all";
    public const string StatusAll = "all";
    public const string StatusExpired = "expired";
    public const string StatusSoon = "soon";
    public const string StatusFresh = "fresh";

    public const string SortExpiryAsc = "expiry-asc";
    public const string SortExpiryDesc = "expiry-desc";
    public const string SortName = "name";

    public static readonly string[] Statuses = [StatusExpired, StatusSoon, StatusFresh];
    public static readonly string[] Sorts = [SortExpiryAsc, SortExpiryDesc, SortName];

    /// <summary>
    /// The bucket a date falls into, using the same window as the status filter
    /// so quick-view counts and list rows can never disagree.
    /// </summary>
    public static string ExpiryBucket(DateOnly expiration, DateOnly today)
        => expiration < today
            ? StatusExpired
            : expiration <= today.AddDays(ExpirySoonDays) ? StatusSoon
            : StatusFresh;

    /// <summary>
    /// Nutri-Score letters map to 1 (best, "a") through 5 (worst, "e").
    /// Anything else — null, "unknown", "not-applicable" — is unscored and
    /// must be left out of an average instead of dragging it either way.
    /// </summary>
    public static int? NutriScoreValue(string? grade)
    {
        if (grade?.Trim() is not { Length: 1 } letter)
            return null;

        var upper = char.ToUpperInvariant(letter[0]);

        return upper is >= 'A' and <= 'E' ? upper - 'A' + 1 : null;
    }

    /// <summary>Nearest grade letter for an average in the 1..5 range.</summary>
    public static string? NutriScoreLetter(double? average)
        => average is not { } value || value < 1 || value > 5
            ? null
            : ((char)('A' + (int)Math.Round(value) - 1)).ToString();
}