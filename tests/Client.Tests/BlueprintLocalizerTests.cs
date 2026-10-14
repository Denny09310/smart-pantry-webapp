using Client.Services;

namespace Client.Tests;

public class BlueprintLocalizerTests
{
    private static BlueprintLocalizer Create() => new(new FakeLocalizer(new Dictionary<string, string>
    {
        ["BbTagRemove"] = "Rimuovi {0}",
        ["BbNotifCount"] = "{0} notifiche",
        ["Close"] = "Chiudi",
    }));

    [Fact]
    public void MappedKey_ReturnsResxValue()
    {
        Assert.Equal("Rimuovi tag", Create()["TagInput.RemoveTag", "tag"]);
    }

    [Fact]
    public void MappedKey_FormatsArguments()
    {
        Assert.Equal("3 notifiche", Create()["NotificationBadge.Count", 3]);
    }

    [Fact]
    public void UnmappedKey_FallsBackToEnglishDefault()
    {
        Assert.Equal("Clear", Create()["MultiSelect.Clear"]);
    }

    [Fact]
    public void UnknownKey_ReturnsKeyItself()
    {
        Assert.Equal("No.Such.Key", Create()["No.Such.Key"]);
    }
}
