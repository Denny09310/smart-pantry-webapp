using Shared.Models;

namespace Server.Tests;

public class NutritionFactsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrBlank_ReturnsNull(string? json)
    {
        Assert.Null(NutritionFactsExtensions.Parse(json!));
        Assert.Null(NutritionFactsExtensions.FromJson(json));
    }

    [Fact]
    public void Parse_Malformed_ReturnsNull()
    {
        Assert.Null(NutritionFactsExtensions.Parse("{not json"));
        Assert.Null(NutritionFactsExtensions.FromJson("{not json"));
    }

    [Fact]
    public void Parse_Valid_RoundTrips()
    {
        var facts = new NutritionFacts(EnergyKcal: 100, Proteins: 5.5);
        var parsed = NutritionFactsExtensions.Parse(facts.ToJson());

        Assert.NotNull(parsed);
        Assert.Equal(facts, parsed);
    }
}
