using System.Text.Json.Serialization;

using Microsoft.Extensions.Caching.Memory;

namespace Server.Services;

/// <summary>
/// English lookup candidates for pantry names via MyMemory (free, keyless).
/// Results are cached for weeks to stay far under the anonymous quota;
/// failures fall back to the original name so suggestions simply narrow
/// instead of breaking.
/// </summary>
internal sealed class TranslationService(
    IHttpClientFactory http,
    IMemoryCache cache,
    ILogger<TranslationService> log)
{
    private static readonly TimeSpan TranslationCacheLifetime = TimeSpan.FromDays(30);
    private static readonly TimeSpan MissCacheLifetime = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<string>> CandidatesAsync(
        string name,
        CancellationToken ct = default)
    {
        var trimmed = name.Trim();

        if (trimmed.Length == 0)
            return [];

        var remote = await TranslateAsync(trimmed, ct);

        return remote is null || string.Equals(remote, trimmed, StringComparison.OrdinalIgnoreCase)
            ? [trimmed]
            : [remote, trimmed];
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

        cache.Set(cacheKey, translated, translated is null ? MissCacheLifetime : TranslationCacheLifetime);

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

        return string.IsNullOrWhiteSpace(best) || IsQuotaRefusal(best) ? null : best.Trim();
    }

    private static bool IsQuotaRefusal(string text)
        => text.Contains("MYMEMORY WARNING", StringComparison.OrdinalIgnoreCase)
            || text.Contains("QUERY LENGTH LIMIT", StringComparison.OrdinalIgnoreCase)
            || text.Contains("INVALID EMAIL", StringComparison.OrdinalIgnoreCase)
            || text.Contains("INVALID TARGET LANGUAGE", StringComparison.OrdinalIgnoreCase);

    private sealed record MyMemoryResponse(
        [property: JsonPropertyName("responseData")] MyMemoryData? Data,
        [property: JsonPropertyName("responseStatus")] int Status,
        [property: JsonPropertyName("matches")] List<MyMemoryMatch>? Matches);

    private sealed record MyMemoryData(
        [property: JsonPropertyName("translatedText")] string? TranslatedText);

    private sealed record MyMemoryMatch(
        [property: JsonPropertyName("translation")] string? Translation);
}
