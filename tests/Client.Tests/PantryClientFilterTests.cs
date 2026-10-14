using Client.Models;
using Client.Services;
using Shared.Models;

namespace Client.Tests;

/// <summary>
/// Pins the client-side mirror of the server query used for granular realtime
/// patching: if this mirror drifts, patched rows land in the wrong place.
/// </summary>
public class PantryClientFilterTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);

    private static PantryItemDto Item(
        string id = "a",
        string name = "Milk",
        string location = "fridge",
        DateOnly? expiration = null)
        => new(id, name, 1, "pcs", location, null, expiration ?? Today.AddDays(3), null, null, null, null);

    [Fact]
    public void MatchesFilters_AllPassesEverything()
    {
        Assert.True(PantryClientFilter.MatchesFilters(
            Item(location: "cellar", name: "zzz", expiration: Today.AddDays(-40)),
            PantryLocations.All, null, PantryRules.StatusAll, Today));
    }

    [Theory]
    [InlineData("fridge", "fridge", true)]
    [InlineData("FRIDGE", "fridge", true)]
    [InlineData("fridge", "pantry", false)]
    public void MatchesFilters_LocationIsExactCaseInsensitive(string filter, string location, bool expected)
    {
        Assert.Equal(expected, PantryClientFilter.MatchesFilters(
            Item(location: location), filter, null, PantryRules.StatusAll, Today));
    }

    [Theory]
    [InlineData("mil", true)]
    [InlineData("MILK", true)]
    [InlineData("oil", false)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    public void MatchesFilters_NameIsSubstringCaseInsensitive(string filter, bool expected)
    {
        Assert.Equal(expected, PantryClientFilter.MatchesFilters(
            Item(name: "Whole Milk"), PantryLocations.All, filter, PantryRules.StatusAll, Today));
    }

    [Theory]
    [InlineData(-1, PantryRules.StatusExpired, true)]
    [InlineData(0, PantryRules.StatusExpired, false)]
    [InlineData(0, PantryRules.StatusSoon, true)]
    [InlineData(7, PantryRules.StatusSoon, true)]
    [InlineData(8, PantryRules.StatusSoon, false)]
    [InlineData(8, PantryRules.StatusFresh, true)]
    [InlineData(-1, PantryRules.StatusFresh, false)]
    public void MatchesFilters_StatusUsesDerivedBucket(int offsetDays, string status, bool expected)
    {
        Assert.Equal(expected, PantryClientFilter.MatchesFilters(
            Item(expiration: Today.AddDays(offsetDays)), PantryLocations.All, null, status, Today));
    }

    [Fact]
    public void Comparer_ExpiryAsc_OrdersByDateThenId()
    {
        var comparer = PantryClientFilter.GetComparer(ItemSort.ExpiryAsc);
        var list = new List<PantryItemDto>
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
        var comparer = PantryClientFilter.GetComparer(ItemSort.ExpiryDesc);
        var list = new List<PantryItemDto>
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
        var comparer = PantryClientFilter.GetComparer(ItemSort.Name);
        var list = new List<PantryItemDto>
        {
            Item("b", name: "banana"),
            Item("a", name: "Apple"),
            Item("c", name: "apple"),
        };

        list.Sort(comparer);

        Assert.Equal(["a", "c", "b"], list.Select(x => x.Id).ToList());
    }
}
