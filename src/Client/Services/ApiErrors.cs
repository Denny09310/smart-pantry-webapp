using System.Text.Json;

using Refit;

namespace Client.Services;

/// <summary>Reads RFC 7807 validation problems from failed Refit calls.</summary>
public static class ApiErrors
{
    public static async Task<Dictionary<string, List<string>>?> ReadValidationErrorsAsync(Exception? error)
    {
        if (error is not ApiException apiError)
            return null;

        try
        {
            using var doc = await apiError.GetContentAsAsync<JsonDocument>();
            if (doc is null || !doc.RootElement.TryGetProperty("errors", out var errors))
                return null;

            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in errors.EnumerateObject())
            {
                result[property.Name] = property.Value.EnumerateArray()
                    .Select(e => e.GetString() ?? "Invalid value.")
                    .ToList();
            }

            return result.Count == 0 ? null : result;
        }
        catch
        {
            return null;
        }
    }
}