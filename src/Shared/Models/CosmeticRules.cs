namespace Shared.Models;

public static class CosmeticRules
{
    public const int ExpirySoonDays = 7;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public const string CategoryAll = "all";
    public const string StatusAll = "all";
    public const string StatusExpired = "expired";
    public const string StatusSoon = "soon";
    public const string StatusGood = "good";

    public const string SortExpiryAsc = "expiry-asc";
    public const string SortExpiryDesc = "expiry-desc";
    public const string SortName = "name";

    public static readonly string[] Statuses = [StatusExpired, StatusSoon, StatusGood];
    public static readonly string[] Sorts = [SortExpiryAsc, SortExpiryDesc, SortName];
}
