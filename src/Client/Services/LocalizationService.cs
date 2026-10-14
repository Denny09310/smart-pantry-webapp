using System.Globalization;

using Client.Resources;

using Microsoft.Extensions.Localization;

namespace Client.Services;

public sealed class LocalizationService(IStringLocalizer<Strings> localizer, Bit.Butil.LocalStorage storage)
{
    private const string StorageKey = "smart-pantry:culture";

    public event Action? CultureChanged;

    public CultureInfo CurrentCulture { get; private set; } = new("en-US");

    public string this[string key] => localizer[key];

    public string this[string key, params object[] args] => localizer[key, args];

    public async Task InitializeAsync()
    {
        try
        {
            var saved = await storage.GetItem<string>(StorageKey);
            if (!string.IsNullOrWhiteSpace(saved))
            {
                var culture = CultureInfo.GetCultureInfo(saved);
                CurrentCulture = culture;
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
        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        try
        {
            await storage.SetItem(StorageKey, cultureName);
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