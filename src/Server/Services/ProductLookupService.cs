using System.Text.Json;
using System.Text.Json.Serialization;

using Shared.Models;

namespace Server.Services;

public interface IProductLookupService
{
    Task<ProductLookupDto?> LookupAsync(string barcode, CancellationToken ct);
}

public sealed class OpenBeautyFactsProductLookupService(HttpClient http) : IProductLookupService
{
    private const string BaseUrl = "https://world.openbeautyfacts.org/api/v0/product/";

    public async Task<ProductLookupDto?> LookupAsync(string barcode, CancellationToken ct)
    {
        var url = $"{BaseUrl}{barcode}.json";

        try
        {
            var response = await http.GetFromJsonAsync<OpenBeautyFactsResponse>(url, ct);

            if (response?.Status != 1 || response.Product is null)
                return null;

            var product = response.Product;

            return new ProductLookupDto(
                Barcode: barcode,
                Name: Truncate(product.ProductName ?? product.ProductNameEn ?? "Unknown Product", 200),
                Brand: NormalizeBrands(product.Brands),
                Quantity: ParseQuantity(product.Quantity),
                Unit: ParseUnit(product.Quantity),
                ImageUrl: product.ImageUrl ?? product.ImageFrontUrl,
                IngredientsText: string.IsNullOrWhiteSpace(product.IngredientsText)
                    ? null
                    : product.IngredientsText.Trim());
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

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>
    /// Open Beauty Facts returns brands as a free-form comma list that often
    /// repeats entries ("Acme, acme, ACME"). Split, trim, drop empties and
    /// dedupe case-insensitively so downstream notes don't accumulate junk.
    /// </summary>
    internal static string? NormalizeBrands(string? brands)
    {
        if (string.IsNullOrWhiteSpace(brands))
            return null;

        var distinct = brands
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinct.Count == 0 ? null : string.Join(", ", distinct);
    }

    /// <summary>
    /// Longer units first: "CL"/"ML"/"LB" all contain "L" and "MG" contains
    /// "G", so the short checks would shadow them ("33CL" parsed as liters).
    /// </summary>
    private static string? ParseUnit(string? quantityStr)
    {
        if (string.IsNullOrWhiteSpace(quantityStr))
            return null;

        var upper = quantityStr.ToUpperInvariant();
        if (upper.Contains("KG")) return "kg";
        if (upper.Contains("MG")) return "mg";
        if (upper.Contains("ML")) return "ml";
        if (upper.Contains("CL")) return "cl";
        if (upper.Contains("LB")) return "lb";
        if (upper.Contains("OZ")) return "oz";
        if (upper.Contains("G")) return "g";
        if (upper.Contains("L")) return "l";

        return null;
    }

    private sealed class OpenBeautyFactsResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("product")]
        public OpenBeautyFactsProduct? Product { get; set; }
    }

    private sealed class OpenBeautyFactsProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("product_name_en")]
        public string? ProductNameEn { get; set; }

        [JsonPropertyName("brands")]
        public string? Brands { get; set; }

        [JsonPropertyName("quantity")]
        public string? Quantity { get; set; }

        [JsonPropertyName("ingredients_text")]
        public string? IngredientsText { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("image_front_url")]
        public string? ImageFrontUrl { get; set; }
    }
}
