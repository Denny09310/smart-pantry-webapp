using System.Net.Http.Json;
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

        string result;
        try
        {
            var url = $"{BaseUrl}?q={Uri.EscapeDataString(text)}&langpair={from}|{to}";
            var response = await http.GetFromJsonAsync<MyMemoryResponse>(url, ct);
            var translated = response?.ResponseData?.TranslatedText;
            result = string.IsNullOrWhiteSpace(translated) ? text : translated.Trim();
        }
        catch (HttpRequestException)
        {
            result = text;
        }
        catch (System.Text.Json.JsonException)
        {
            result = text;
        }
        catch (OperationCanceledException)
        {
            result = text;
        }

        cache.Set(key, result, TimeSpan.FromDays(30));
        return result;
    }

    private sealed class MyMemoryResponse
    {
        [JsonPropertyName("responseData")]
        public ResponseData? ResponseData { get; set; }
    }

    private sealed class ResponseData
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}
