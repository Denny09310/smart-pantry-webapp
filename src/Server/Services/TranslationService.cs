using System.Text.Json.Serialization;

using Microsoft.Extensions.Caching.Memory;

namespace Server.Services;

public interface ITranslationService
{
    /// <summary>Translates short text via MyMemory; returns the input on failure.</summary>
    Task<string> TranslateAsync(string text, string from, string to, CancellationToken ct);
}

/// <summary>MyMemory: free, keyless translation with a shared quota.
/// Every result is cached in memory — ingredient vocabulary repeats heavily,
/// so steady-state traffic barely touches the quota.</summary>
public sealed class MyMemoryTranslationService(HttpClient http, IMemoryCache cache) : ITranslationService
{
    private const string BaseUrl = "https://api.mymemory.translated.net/get";

    public async Task<string> TranslateAsync(string text, string from, string to, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text) || from == to)
            return text;

        var key = $"mymemory:{from}:{to}:{text.Trim().ToLowerInvariant()}";
        if (cache.TryGetValue(key, out string? cached) && cached is not null)
            return cached;

        // Only successful translations are cached: a transient failure must
        // fall back to the input every time, never stick for 30 days.
        var translated = await FetchAsync(text, from, to, ct);
        if (translated is null)
            return text;

        cache.Set(key, translated, TimeSpan.FromDays(30));
        return translated;
    }

    private async Task<string?> FetchAsync(string text, string from, string to, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}?q={Uri.EscapeDataString(text)}&langpair={from}|{to}";
            var response = await http.GetFromJsonAsync<MyMemoryResponse>(url, ct);
            if (response?.ResponseStatus != 200)
                return null;
            var translated = response?.ResponseData?.TranslatedText;
            return string.IsNullOrWhiteSpace(translated) ? null : translated.Trim();
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private sealed class MyMemoryResponse
    {
        [JsonPropertyName("responseStatus")]
        public int ResponseStatus { get; set; }

        [JsonPropertyName("responseData")]
        public ResponseData? ResponseData { get; set; }
    }

    private sealed class ResponseData
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}