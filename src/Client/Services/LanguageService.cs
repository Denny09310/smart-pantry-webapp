using System.Globalization;

using Bit.Butil;

using Microsoft.AspNetCore.Components;

using Shared.Resources;

namespace Client.Services;

/// <summary>
/// UI language: the saved choice wins, else the browser's preferred language
/// when supported, else English. Switching persists and reloads so every
/// string (including startup-configured ones) re-resolves.
/// </summary>
internal sealed class LanguageService(
    LocalStorage storage,
    Navigator navigator,
    NavigationManager nav,
    ILogger<LanguageService> log)
{
    private const string StorageKey = "smart-pantry.language";

    public string Current { get; private set; } = UIStrings.DefaultLanguage;

    public async Task<CultureInfo> ResolveAsync()
    {
        try
        {
            var saved = await storage.GetItem(StorageKey);
            var pick = UIStrings.Normalize(saved);

            if (!UIStrings.IsSupported(saved))
            {
                var browser = await navigator.GetLanguages();

                foreach (var tag in browser)
                {
                    var two = tag.Split('-', '_')[0];

                    if (UIStrings.IsSupported(two))
                    {
                        pick = two.ToLowerInvariant();
                        break;
                    }
                }
            }

            Current = pick;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Language resolution failed.");
            Current = UIStrings.DefaultLanguage;
        }

        return new CultureInfo(Current);
    }

    public async Task SetAsync(string language)
    {
        var pick = UIStrings.Normalize(language);

        try
        {
            await storage.SetItem(StorageKey, pick);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Language persistence failed.");
        }

        Current = pick;
        nav.NavigateTo(nav.Uri, forceLoad: true);
    }
}
