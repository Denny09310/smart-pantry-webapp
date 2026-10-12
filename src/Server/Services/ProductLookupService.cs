using System.Text.Json;
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
                EnergyKcal: nutriments?.EnergyKcal100g,
                Proteins: nutriments?.Proteins100g,
                Carbohydrates: nutriments?.Carbohydrates100g,
                Sugars: nutriments?.Sugars100g,
                Fat: nutriments?.Fat100g,
                SaturatedFat: nutriments?.SaturatedFat100g,
                Salt: nutriments?.Salt100g);

            return new ProductLookupDto(
                Barcode: barcode,
                Name: product.ProductName ?? product.ProductNameEn ?? "Unknown Product",
                Brand: product.Brands,
                Quantity: ParseQuantity(product.Quantity),
                Unit: ParseUnit(product.Quantity),
                NutriScore: product.NutriscoreGrade?.ToUpperInvariant(),
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
        public int Status { get; set; }
        public OpenFoodFactsProduct? Product { get; set; }
    }

    private sealed class OpenFoodFactsProduct
    {
        public string? ProductName { get; set; }
        public string? ProductNameEn { get; set; }
        public string? Brands { get; set; }
        public string? Quantity { get; set; }
        public string? NutriscoreGrade { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageFrontUrl { get; set; }
        public OpenFoodFactsNutriments? Nutriments { get; set; }
    }

    private sealed class OpenFoodFactsNutriments
    {
        public double? EnergyKcal100g { get; set; }
        public double? Proteins100g { get; set; }
        public double? Carbohydrates100g { get; set; }
        public double? Sugars100g { get; set; }
        public double? Fat100g { get; set; }
        public double? SaturatedFat100g { get; set; }
        public double? Salt100g { get; set; }
    }
}