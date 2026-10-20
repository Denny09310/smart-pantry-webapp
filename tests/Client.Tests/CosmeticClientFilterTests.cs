using Client.Models;
using Client.Services;
using Shared.Models;

namespace Client.Tests;

/// <summary>
/// Pins the client-side mirror of the server query used for granular realtime
/// patching: if this mirror drifts, patched rows land in the wrong place.
/// </summary>
public class CosmeticClientFilterTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);

    private static CosmeticItemDto Item(
        string id = "a",
        string name = "Milk",
        string category = "face",
        DateOnly? expiration = null)
        => new(id, name, 1, "pcs", category, null, expiration ?? Today.AddDays(3), null, null);

    [Fact]
    public void MatchesFilters_AllPassesEverything()
    {
        Assert.True(CosmeticClientFilter.MatchesFilters(
            Item(category: "body", name: "zzz", expiration: Today.AddDays(-40)),
            CosmeticCategories.All, null, CosmeticRules.StatusAll, Today));
    }

    [Theory]
    [InlineData("face", "face", true)]
    [InlineData("FACE", "face", true)]
    [InlineData("face", "hair", false)]
    public void MatchesFilters_CategoryIsExactCaseInsensitive(string filter, string category, bool expected)
    {
        Assert.Equal(expected, CosmeticClientFilter.MatchesFilters(
            Item(category: category), filter, null, CosmeticRules.StatusAll, Today));
    }

    [Theory]
    [InlineData("mil", true)]
    [InlineData("MILK", true)]
    [InlineData("oil", false)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    public void MatchesFilters_NameIsSubstringCaseInsensitive(string filter, bool expected)
    {
        Assert.Equal(expected, CosmeticClientFilter.MatchesFilters(
            Item(name: "Whole Milk"), CosmeticCategories.All, filter, CosmeticRules.StatusAll, Today));
    }

    [Theory]
    [InlineData(-1, CosmeticRules.StatusExpired, true)]
    [InlineData(0, CosmeticRules.StatusExpired, false)]
    [InlineData(0, CosmeticRules.StatusSoon, true)]
    [InlineData(7, CosmeticRules.StatusSoon, true)]
    [InlineData(8, CosmeticRules.StatusSoon, false)]
    [InlineData(8, CosmeticRules.StatusGood, true)]
    [InlineData(-1, CosmeticRules.StatusGood, false)]
    public void MatchesFilters_StatusUsesDerivedBucket(int offsetDays, string status, bool expected)
    {
        Assert.Equal(expected, CosmeticClientFilter.MatchesFilters(
            Item(expiration: Today.AddDays(offsetDays)), CosmeticCategories.All, null, status, Today));
    }

    [Fact]
    public void Comparer_ExpiryAsc_OrdersByDateThenId()
    {
        var comparer = CosmeticClientFilter.GetComparer(ItemSort.ExpiryAsc);
        var list = new List<CosmeticItemDto>
        {
            Item("b", expiration: Today.AddDays(5)),
            Item("a", expiration: Today.AddDays(1)),
            Item("c", expiration: Today.AddDays(1)),
        };

        list.Sort(comparer);

        Assert.Equal(["a", "c", "b"], list.Select(x => x.Id).ToList());
    }

    [Fact]
    public void Comparer_ExpiryDesc_OrdersByDateDescThenId()
    {
        var comparer = CosmeticClientFilter.GetComparer(ItemSort.ExpiryDesc);
        var list = new List<CosmeticItemDto>
        {
            Item("b", expiration: Today.AddDays(1)),
            Item("a", expiration: Today.AddDays(5)),
            Item("c", expiration: Today.AddDays(5)),
        };

        list.Sort(comparer);

        Assert.Equal(["a", "c", "b"], list.Select(x => x.Id).ToList());
    }

    [Fact]
    public void Comparer_Name_OrdersCaseInsensitiveThenId()
    {
        var comparer = CosmeticClientFilter.GetComparer(ItemSort.Name);
        var list = new List<CosmeticItemDto>
        {
            Item("b", name: "banana"),
            Item("a", name: "Apple"),
            Item("c", name: "apple"),
        };

        list.Sort(comparer);

        Assert.Equal(["a", "c", "b"], list.Select(x => x.Id).ToList());
    }
}
