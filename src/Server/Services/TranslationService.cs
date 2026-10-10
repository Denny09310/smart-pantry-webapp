using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Caching.Memory;

namespace Server.Services;

/// <summary>
/// English lookup candidates for pantry names. The curated dictionary answers
/// staples instantly and offline; anything else goes to MyMemory (free,
/// keyless) when the caller asks for translation, cached for weeks to stay
/// far under the anonymous quota. Failures fall back to the original name.
/// </summary>
internal sealed class TranslationService(
    IHttpClientFactory http,
    IMemoryCache cache,
    ILogger<TranslationService> log)
{
    private static readonly TimeSpan TranslationCacheLifetime = TimeSpan.FromDays(30);

    public async Task<IReadOnlyList<string>> CandidatesAsync(
        string name,
        bool translate,
        CancellationToken ct = default)
    {
        var trimmed = name.Trim();

        if (trimmed.Length == 0)
            return [];

        var local = PantryTranslator.Candidates(trimmed);

        // Dictionary hit already yields a translation; nothing to do.
        if (!translate || local.Count > 1)
            return local;

        var remote = await TranslateAsync(trimmed, ct);

        if (remote is null || string.Equals(remote, trimmed, StringComparison.OrdinalIgnoreCase))
            return local;

        return [remote, trimmed];
    }

    private async Task<string?> TranslateAsync(string text, CancellationToken ct)
    {
        var cacheKey = $"translate:it:{text.ToLowerInvariant()}";

        if (cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        string? translated = null;

        try
        {
            translated = await FetchTranslationAsync(text, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Translation of {Text} failed.", text);
        }

        // Cache misses too (null): an outage shouldn't retry every call.
        cache.Set(cacheKey, translated, TranslationCacheLifetime);

        return translated;
    }

    private async Task<string?> FetchTranslationAsync(string text, CancellationToken ct)
    {
        var client = http.CreateClient("MyMemory");

        using var response = await client.GetAsync(
            $"get?q={Uri.EscapeDataString(text)}&langpair=it|en", ct);

        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<MyMemoryResponse>(ct);

        var best = payload?.Matches?.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Translation))?.Translation
            ?? payload?.Data?.TranslatedText;

        if (string.IsNullOrWhiteSpace(best) || IsQuotaRefusal(best))
            return null;

        return best.Trim();
    }

    private static bool IsQuotaRefusal(string text)
        => text.Contains("MYMEMORY WARNING", StringComparison.OrdinalIgnoreCase)
            || text.Contains("QUERY LENGTH LIMIT", StringComparison.OrdinalIgnoreCase)
            || text.Contains("INVALID", StringComparison.OrdinalIgnoreCase);

    private sealed record MyMemoryResponse(
        [property: JsonPropertyName("responseData")] MyMemoryData? Data,
        [property: JsonPropertyName("responseStatus")] int Status,
        [property: JsonPropertyName("matches")] List<MyMemoryMatch>? Matches);

    private sealed record MyMemoryData(
        [property: JsonPropertyName("translatedText")] string? TranslatedText);

    private sealed record MyMemoryMatch(
        [property: JsonPropertyName("translation")] string? Translation);
}
