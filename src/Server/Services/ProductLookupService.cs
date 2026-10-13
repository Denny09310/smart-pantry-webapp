using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Shared.Models;

namespace Server.Services;

public interface IProductLookupService
{
    Task<ProductLookupDto?> LookupAsync(string barcode, CancellationToken ct);
}

public sealed class OpenFoodFactsProductLookupService(HttpClient http) : IProductLookupService
{
    private const string BaseUrl = "https://world.openfoodfacts.org/api/v0/product/";

    public async Task<ProductLookupDto?> LookupAsync(string barcode, CancellationToken ct)
    {
        var url = $"{BaseUrl}{barcode}.json";

        try
        {
            var response = await http.GetFromJsonAsync<OpenFoodFactsResponse>(url, ct);

            if (response?.Status != 1 || response.Product is null)
                return null;

            var product = response.Product;
            var nutriments = product.Nutriments;

            var nutrition = new NutritionFacts(
                EnergyKcal: ToDouble(nutriments?.EnergyKcal),
                Proteins: ToDouble(nutriments?.Proteins),
                Carbohydrates: ToDouble(nutriments?.Carbohydrates),
                Sugars: ToDouble(nutriments?.Sugars),
                Fat: ToDouble(nutriments?.Fat),
                SaturatedFat: ToDouble(nutriments?.SaturatedFat),
                Salt: ToDouble(nutriments?.Salt));

            return new ProductLookupDto(
                Barcode: barcode,
                Name: Truncate(product.ProductName ?? product.ProductNameEn ?? "Unknown Product", 200),
                Brand: product.Brands,
                Quantity: ParseQuantity(product.Quantity),
                Unit: ParseUnit(product.Quantity),
                NutriScore: NormalizeNutriScore(product.NutriscoreGrade),
                Nutrition: nutrition,
                ImageUrl: product.ImageUrl ?? product.ImageFrontUrl);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            // Timeout or caller cancellation: no product, not a server error.
            return null;
        }
    }

    private static double? ParseQuantity(string? quantityStr)
    {
        if (string.IsNullOrWhiteSpace(quantityStr))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(quantityStr, @"([\d.,]+)");
        if (match.Success && double.TryParse(match.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;

        return null;
    }

    /// <summary>
    /// OFF grades are single a–e letters; anything else ("unknown",
    /// "not-applicable") becomes null so the save never trips validation.
    /// </summary>
    private static string? NormalizeNutriScore(string? grade)
    {
        var letter = grade?.Trim().ToUpperInvariant();
        return letter is { Length: 1 } value && value[0] is >= 'A' and <= 'E'
            ? value
            : null;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static string? ParseUnit(string? quantityStr)
    {
        if (string.IsNullOrWhiteSpace(quantityStr))
            return null;

        var upper = quantityStr.ToUpperInvariant();
        if (upper.Contains("KG")) return "kg";
        if (upper.Contains("G") && !upper.Contains("MG")) return "g";
        if (upper.Contains("MG")) return "mg";
        if (upper.Contains("L") && !upper.Contains("ML")) return "l";
        if (upper.Contains("ML")) return "ml";
        if (upper.Contains("CL")) return "cl";
        if (upper.Contains("OZ")) return "oz";
        if (upper.Contains("LB") || upper.Contains("LBS")) return "lb";

        return null;
    }

    private sealed class OpenFoodFactsResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("product")]
        public OpenFoodFactsProduct? Product { get; set; }
    }

    private sealed class OpenFoodFactsProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("product_name_en")]
        public string? ProductNameEn { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("quantity")]
        public string? Quantity { get; set; }

        [JsonPropertyName("nutriscore_grade")]
        public string? NutriscoreGrade { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }

        [JsonPropertyName("nutriments")]
        public OpenFoodFactsNutriments? Nutriments { get; set; }
    }

    private sealed class OpenFoodFactsNutriments
    {
        // OFF sends numbers, numeric strings, or "" per key — never assume.
        [JsonPropertyName("energy-kcal_100g")]
        public JsonElement? EnergyKcal { get; set; }

        [JsonPropertyName("proteins_100g")]
        public JsonElement? Proteins { get; set; }

        [JsonPropertyName("carbohydrates_100g")]
        public JsonElement? Carbohydrates { get; set; }

        [JsonPropertyName("sugars_100g")]
        public JsonElement? Sugars { get; set; }

        [JsonPropertyName("fat_100g")]
        public JsonElement? Fat { get; set; }

        [JsonPropertyName("saturated-fat_100g")]
        public JsonElement? SaturatedFat { get; set; }

        [JsonPropertyName("salt_100g")]
        public JsonElement? Salt { get; set; }
    }

    private static double? ToDouble(JsonElement? element)
    {
        if (element is not { } value
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String
            && double.TryParse(
                value.GetString()?.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed))
            return parsed;

        return null;
    }
}