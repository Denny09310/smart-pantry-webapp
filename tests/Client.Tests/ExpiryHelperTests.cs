using BlazorBlueprint.Components;
using Client.Services;

namespace Client.Tests;

public class ExpiryHelperTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);

    [Theory]
    [InlineData(-30, ExpiryStatus.Expired)]
    [InlineData(-1, ExpiryStatus.Expired)]
    [InlineData(0, ExpiryStatus.ExpiringSoon)]
    [InlineData(1, ExpiryStatus.ExpiringSoon)]
    [InlineData(7, ExpiryStatus.ExpiringSoon)]
    [InlineData(8, ExpiryStatus.Fresh)]
    [InlineData(365, ExpiryStatus.Fresh)]
    public void GetStatus_ClassifiesBoundaries(int offsetDays, ExpiryStatus expected)
    {
        Assert.Equal(expected, ExpiryHelper.GetStatus(Today.AddDays(offsetDays), Today));
    }

    [Theory]
    [InlineData(ExpiryStatus.Expired, "StatusExpired")]
    [InlineData(ExpiryStatus.ExpiringSoon, "StatusExpiringSoon")]
    [InlineData(ExpiryStatus.Fresh, "StatusFresh")]
    public void Label_UsesLocalizerKey(ExpiryStatus status, string expectedKey)
    {
        var loc = TestLoc.Create(new Dictionary<string, string> { [expectedKey] = $"[{expectedKey}]" });

        Assert.Equal($"[{expectedKey}]", ExpiryHelper.Label(status, loc));
    }

    [Theory]
    [InlineData(ExpiryStatus.Expired, BadgeVariant.Destructive)]
    [InlineData(ExpiryStatus.ExpiringSoon, BadgeVariant.Warning)]
    [InlineData(ExpiryStatus.Fresh, BadgeVariant.SoftSuccess)]
    public void Badge_MapsStatus(ExpiryStatus status, BadgeVariant expected)
    {
        Assert.Equal(expected, ExpiryHelper.Badge(status));
    }

    [Fact]
    public void GetBanner_ExpiredYesterday_UsesSingularKey()
    {
        var loc = TestLoc.Create(new Dictionary<string, string> { ["ExpiredYesterday"] = "yesterday!" });

        var banner = ExpiryHelper.GetBanner(Today.AddDays(-1), Today, loc);

        Assert.Equal("yesterday!", banner.Text);
        Assert.Equal(BadgeVariant.SoftDestructive, banner.Variant);
    }

    [Fact]
    public void GetBanner_ExpiredDays_FormatsCount()
    {
        var loc = TestLoc.Create(new Dictionary<string, string> { ["ExpiredDaysAgo"] = "gone {0}d" });

        var banner = ExpiryHelper.GetBanner(Today.AddDays(-3), Today, loc);

        Assert.Equal("gone 3d", banner.Text);
    }

    [Fact]
    public void GetBanner_Today_IsWarning()
    {
        var loc = TestLoc.Create(new Dictionary<string, string> { ["ExpiresToday"] = "today!" });

        var banner = ExpiryHelper.GetBanner(Today, Today, loc);

        Assert.Equal("today!", banner.Text);
        Assert.Equal(BadgeVariant.SoftWarning, banner.Variant);
    }

    [Fact]
    public void GetBanner_OneDayLeft_UsesSingularKey()
    {
        var loc = TestLoc.Create(new Dictionary<string, string> { ["UseSoonOneDay"] = "one left!" });

        Assert.Equal("one left!", ExpiryHelper.GetBanner(Today.AddDays(1), Today, loc).Text);
    }

    [Fact]
    public void GetBanner_SoonAndFresh_FormatCounts()
    {
        var loc = TestLoc.Create(new Dictionary<string, string>
        {
            ["UseSoonDaysLeft"] = "soon {0}",
            ["FreshDaysLeft"] = "fresh {0}",
        });

        Assert.Equal("soon 5", ExpiryHelper.GetBanner(Today.AddDays(5), Today, loc).Text);
        Assert.Equal("fresh 30", ExpiryHelper.GetBanner(Today.AddDays(30), Today, loc).Text);
    }
}
