using System.Globalization;

using Client.Services;

namespace Client.Tests;

public class LocalizationServiceTests
{
    [Fact]
    public async Task InitializeAsync_WithoutSavedCulture_FollowsBrowserLanguage()
    {
        // FakeJsRuntime throws, so no saved culture exists: fresh-install path.
        using var _ = new AmbientCulture("it-IT");

        var service = TestLoc.Create();
        await service.InitializeAsync();

        Assert.Equal("it-IT", service.CurrentCulture.Name);
    }

    [Fact]
    public async Task InitializeAsync_UnsupportedBrowserLanguage_FallsBackToEnglish()
    {
        using var _ = new AmbientCulture("fr-FR");

        var service = TestLoc.Create();
        await service.InitializeAsync();

        Assert.Equal("en-US", service.CurrentCulture.Name);
    }

    [Fact]
    public async Task SetCultureAsync_SwitchesCurrentCulture()
    {
        var service = TestLoc.Create();

        await service.SetCultureAsync("it-IT");

        Assert.Equal("it-IT", service.CurrentCulture.Name);
    }

    /// <summary>Restores the ambient UI culture on dispose (parallel-safe).</summary>
    private sealed class AmbientCulture : IDisposable
    {
        private readonly CultureInfo? _previous = CultureInfo.DefaultThreadCurrentUICulture;

        public AmbientCulture(string name)
            => CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.DefaultThreadCurrentUICulture = _previous;
    }
}
