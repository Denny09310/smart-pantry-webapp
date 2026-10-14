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
}