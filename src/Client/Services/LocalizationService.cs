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
        string? saved = null;
        try
        {
            saved = await storage.GetItem<string>(StorageKey);
        }
        catch
        {
            // Ignore localStorage errors
        }

        // No saved preference (fresh install): follow the browser language so
        // labels and CurrentCulture agree. Resource lookup already uses the
        // runtime UI culture; without this the app speaks Italian while the
        // API language stays English.
        ApplyCulture(CultureFromNameOrNull(saved) ?? ClosestSupported(CultureInfo.CurrentUICulture));
    }

    public async Task SetCultureAsync(string cultureName)
    {
        ApplyCulture(ClosestSupported(CultureInfo.GetCultureInfo(cultureName)));

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

    private void ApplyCulture(CultureInfo culture)
    {
        CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private static CultureInfo? CultureFromNameOrNull(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static CultureInfo ClosestSupported(CultureInfo culture)
        => SupportedCultures.FirstOrDefault(x => x.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
            ?? SupportedCultures.FirstOrDefault(x => x.TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName)
            ?? SupportedCultures[0];

    public static readonly CultureInfo[] SupportedCultures =
    [
        new("en-US"),
        new("it-IT")
    ];
}