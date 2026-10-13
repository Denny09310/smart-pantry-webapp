using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Client.Resources;
using System.Globalization;

namespace Client.Services;

public sealed class LocalizationService(IStringLocalizer<Strings> localizer, IJSRuntime js)
{
    private const string StorageKey = "smart-pantry:culture";
    private CultureInfo _currentCulture = new("en-US");

    public event Action? CultureChanged;

    public CultureInfo CurrentCulture => _currentCulture;

    public string this[string key] => localizer[key];

    public string this[string key, params object[] args] => localizer[key, args];

    public async Task InitializeAsync()
    {
        try
        {
            var saved = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(saved))
            {
                var culture = CultureInfo.GetCultureInfo(saved);
                _currentCulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    public async Task SetCultureAsync(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        _currentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, cultureName);
        }
        catch
        {
            // Ignore localStorage errors
        }

        CultureChanged?.Invoke();
    }

    public static readonly CultureInfo[] SupportedCultures =
    [
        new("en-US"),
        new("it-IT")
    ];
}