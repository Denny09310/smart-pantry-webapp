using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using Server.Data;

using Shared.Models;

namespace Server.Tests;

public class PantryStatsTests(PantryApiFactory factory) : IClassFixture<PantryApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Stats_CountsEveryExpiryBucket()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expired = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-expired-{Guid.CreateVersion7()}", today.AddDays(-1));
        var todayItem = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-today-{Guid.CreateVersion7()}", today);
        var lastDayOfWindow = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-edge-{Guid.CreateVersion7()}", today.AddDays(PantryRules.ExpirySoonDays));
        var fresh = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-fresh-{Guid.CreateVersion7()}", today.AddDays(PantryRules.ExpirySoonDays + 1));

        try
        {
            var stats = await GetStatsAsync();

            // The window is inclusive at both ends, exactly like the status filter.
            Assert.Equal(1, stats.Expired);
            Assert.Equal(2, stats.ExpiringSoon);
            Assert.Equal(1, stats.Fresh);
            Assert.Equal(4, stats.Total);
        }
        finally
        {
            await DeleteAllAsync(expired, todayItem, lastDayOfWindow, fresh);
        }
    }

    [Fact]
    public async Task Stats_AveragesNutriScore_AndIgnoresUnscoredItems()
    {
        var best = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-a-{Guid.CreateVersion7()}", nutriScore: "A");
        var worst = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-e-{Guid.CreateVersion7()}", nutriScore: "E");
        // Hand-entered items carry no score and must not drag the average.
        var unscored = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-none-{Guid.CreateVersion7()}");

        try
        {
            var stats = await GetStatsAsync();

            Assert.Equal(2, stats.ScoredItems);
            Assert.Equal(3, stats.Total);
            Assert.Equal(3d, stats.AverageNutriScore!.Value, 3);
            Assert.Equal("C", stats.NutriScoreGrade);
        }
        finally
        {
            await DeleteAllAsync(best, worst, unscored);
        }
    }

    [Fact]
    public async Task Stats_NothingScored_ReturnsNoGoodness()
    {
        var item = await PantryTestHelpers.SeedItemAsync(factory.Services, $"stats-unscored-{Guid.CreateVersion7()}");

        try
        {
            var stats = await GetStatsAsync();

            Assert.Equal(1, stats.Total);
            Assert.Equal(0, stats.ScoredItems);
            Assert.Null(stats.AverageNutriScore);
            Assert.Null(stats.NutriScoreGrade);
        }
        finally
        {
            await DeleteAllAsync(item);
        }
    }

    [Fact]
    public async Task Stats_EmptyPantry_ReturnsZeroes()
    {
        var stats = await GetStatsAsync();

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.Fresh);
        Assert.Equal(0, stats.ExpiringSoon);
        Assert.Equal(0, stats.Expired);
        Assert.Null(stats.NutriScoreGrade);
    }

    private async Task<PantryStatsDto> GetStatsAsync()
    {
        var response = await _client.GetAsync("/api/pantry/stats");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<PantryStatsDto>();
        Assert.NotNull(stats);
        return stats;
    }

    private async Task DeleteAllAsync(params Server.Data.Entities.PantryItem[] items)
    {
        foreach (var item in items)
            await PantryTestHelpers.DeleteItemAsync(factory.Services, item.Id);
    }
}

public class PantryRulesTests
{
    [Theory]
    [InlineData("A", 1)]
    [InlineData("b", 2)]
    [InlineData(" C ", 3)]
    [InlineData("d", 4)]
    [InlineData("E", 5)]
    public void NutriScoreValue_MapsGradesToOneThroughFive(string grade, int expected)
        => Assert.Equal(expected, PantryRules.NutriScoreValue(grade));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("not-applicable")]
    [InlineData("AB")]
    public void NutriScoreValue_IgnoresAnythingButASingleGradeLetter(string? grade)
        => Assert.Null(PantryRules.NutriScoreValue(grade));

    [Theory]
    [InlineData(1, "A")]
    [InlineData(2.4, "B")]
    [InlineData(3, "C")]
    [InlineData(4.6, "E")]
    [InlineData(5, "E")]
    public void NutriScoreLetter_RoundsToTheNearestGrade(double average, string expected)
        => Assert.Equal(expected, PantryRules.NutriScoreLetter(average));

    [Theory]
    [InlineData(null)]
    [InlineData(0.5)]
    [InlineData(5.5)]
    public void NutriScoreLetter_RejectsValuesOutsideTheScale(double? average)
        => Assert.Null(PantryRules.NutriScoreLetter(average));

    [Fact]
    public void ExpiryBucket_UsesTheSameWindowAsTheStatusFilter()
    {
        var today = new DateOnly(2026, 10, 17);

        Assert.Equal(PantryRules.StatusExpired, PantryRules.ExpiryBucket(today.AddDays(-1), today));
        Assert.Equal(PantryRules.StatusSoon, PantryRules.ExpiryBucket(today, today));
        Assert.Equal(PantryRules.StatusSoon, PantryRules.ExpiryBucket(today.AddDays(PantryRules.ExpirySoonDays), today));
        Assert.Equal(PantryRules.StatusFresh, PantryRules.ExpiryBucket(today.AddDays(PantryRules.ExpirySoonDays + 1), today));
    }
}
