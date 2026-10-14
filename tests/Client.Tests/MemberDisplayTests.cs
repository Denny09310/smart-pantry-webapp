using Client.Models;

namespace Client.Tests;

public class MemberDisplayTests
{
    [Theory]
    [InlineData("Alex", "AL")]
    [InlineData("A", "A")]
    [InlineData("Maria Rossi", "MR")]
    [InlineData("maria rossi", "MR")]
    [InlineData("  Alex   Bob  ", "AB")]
    [InlineData("", "?")]
    public void Initials_FormatsNames(string name, string expected)
    {
        Assert.Equal(expected, MemberDisplay.Initials(name));
    }

    [Theory]
    [InlineData("#ffffff", "#000000")] // white -> black text
    [InlineData("#000000", "#ffffff")] // black -> white text
    [InlineData("#3b82f6", "#ffffff")] // saturated blue is dark
    [InlineData("#eab308", "#000000")] // yellow is light
    [InlineData(null, "#ffffff")] // unknown -> safe white
    [InlineData("", "#ffffff")]
    [InlineData("red", "#ffffff")] // not a hex color -> safe white
    public void ForegroundFor_PicksReadableColor(string? background, string expected)
    {
        Assert.Equal(expected, MemberDisplay.ForegroundFor(background));
    }
}
