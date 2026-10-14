using System.Globalization;
using Client.Resources;
using Client.Services;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Client.Tests;

/// <summary>Dictionary-backed localizer + inert JS runtime for label/banner tests.</summary>
internal sealed class FakeLocalizer(Dictionary<string, string> values) : IStringLocalizer<Strings>
{
    public LocalizedString this[string name]
        => new(name, values.TryGetValue(name, out var value) ? value : name, resourceNotFound: !values.ContainsKey(name));

    public LocalizedString this[string name, params object[] args]
        => new(name, string.Format(CultureInfo.InvariantCulture, this[name].Value, args), resourceNotFound: false);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => values.Select(kv => new LocalizedString(kv.Key, kv.Value, resourceNotFound: false));

    [Obsolete("Use CurrentCulture/CurrentUICulture instead.")]
    public IStringLocalizer WithCulture(CultureInfo culture) => this;
}

internal sealed class FakeJsRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => throw new NotImplementedException("JS is not available in unit tests.");

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw new NotImplementedException("JS is not available in unit tests.");
}

internal static class TestLoc
{
    public static LocalizationService Create(Dictionary<string, string>? values = null)
        => new(new FakeLocalizer(values ?? []), new Bit.Butil.LocalStorage(new FakeJsRuntime()));
}
