using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Services;

/// <summary>
/// Open Food Facts lookup with a local cache: cache first, upstream on
/// miss, null when unknown or unreachable (lookup failure never blocks
/// manual creation).
/// </summary>
internal sealed class ProductLookupService(
    IHttpClientFactory http,
    ApplicationDbContext db,
    ILogger<ProductLookupService> log)
{
    public async Task<ProductLookupDto?> LookupAsync(string barcode, CancellationToken ct = default)
    {
        var cached = await db.BarcodeProducts
            .FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

        if (cached is not null && cached.IngredientsText is not null)
            return ToDto(cached);

        ProductLookupDto? fresh = null;

        try
        {
            fresh = await FetchAsync(barcode, ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            log.LogWarning(ex, "Product lookup for {Barcode} failed.", barcode);
        }

        if (fresh is null)
            return cached is null ? null : ToDto(cached);

        if (cached is null)
        {
            var entry = new BarcodeProduct
            {
                Barcode = fresh.Barcode,
                Name = fresh.Name,
                Brand = fresh.Brand,
                Quantity = fresh.Quantity,
                ImageUrl = fresh.ImageUrl,
            };

            ApplyDetails(entry, fresh);
            db.BarcodeProducts.Add(entry);
        }
        else
        {
            // Row cached before details existed: backfill, still tracking.
            ApplyDetails(cached, fresh);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost a concurrent first-scan race: the winner's row is canonical.
            db.ChangeTracker.Clear();
        }

        return fresh;
    }

    private async Task<ProductLookupDto?> FetchAsync(string barcode, CancellationToken ct)
    {
        var client = http.CreateClient("OpenFoodFacts");

        using var response = await client.GetAsync(
            $"api/v2/product/{barcode}.json?fields=code,product_name,brands,quantity,image_url,ingredients_text,nutriments,nutriscore_grade",
            ct);

        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<OffProductResponse>(ct);

        if (payload?.Status != 1 || string.IsNullOrWhiteSpace(payload.Product?.ProductName))
            return null;

        var product = payload.Product;

        return new ProductLookupDto(
            barcode,
            product.ProductName.Trim(),
            product.Brands,
            product.Quantity,
            product.ImageUrl,
            string.IsNullOrWhiteSpace(product.IngredientsText) ? null : product.IngredientsText.Trim(),
            product.NutriscoreGrade?.Trim().ToLowerInvariant(),
            ToNutrition(product.Nutriments));
    }

    /// <summary>
    /// OFF nutriment keys are stable (`fat_100g`, `saturated-fat_100g`, …)
    /// but values may arrive as numbers or numeric strings; unknown keys
    /// are ignored. Energy prefers the explicit kcal field: plain
    /// `energy_100g` is kJ unless `energy_unit` says otherwise.
    /// </summary>
    private static NutritionFactsDto? ToNutrition(Dictionary<string, JsonElement>? nutriments)
    {
        if (nutriments is null || nutriments.Count == 0)
            return null;

        var facts = new NutritionFactsDto(
            EnergyKcal(nutriments),
            GetDouble(nutriments, "fat_100g"),
            GetDouble(nutriments, "saturated-fat_100g"),
            GetDouble(nutriments, "carbohydrates_100g"),
            GetDouble(nutriments, "sugars_100g"),
            GetDouble(nutriments, "fiber_100g"),
            GetDouble(nutriments, "proteins_100g"),
            GetDouble(nutriments, "salt_100g"));

        return facts is
        {
            EnergyKcal100g: null,
            Fat100g: null,
            SaturatedFat100g: null,
            Carbohydrates100g: null,
            Sugars100g: null,
            Fiber100g: null,
            Proteins100g: null,
            Salt100g: null,
        }
            ? null
            : facts;
    }

    private static double? EnergyKcal(Dictionary<string, JsonElement> nutriments)
    {
        if (GetDouble(nutriments, "energy-kcal_100g") is { } kcal)
            return kcal;

        if (GetDouble(nutriments, "energy_100g") is not { } energy)
            return null;

        if (nutriments.TryGetValue("energy_unit", out var unit)
            && unit.ValueKind == JsonValueKind.String
            && string.Equals(unit.GetString(), "kcal", StringComparison.OrdinalIgnoreCase))
            return energy;

        if (unit.ValueKind == JsonValueKind.String
            && string.Equals(unit.GetString(), "kj", StringComparison.OrdinalIgnoreCase))
            return energy / 4.184;

        // Unit unknown: never guess between kJ and kcal.
        return null;
    }

    private static double? GetDouble(Dictionary<string, JsonElement> nutriments, string key)
    {
        if (!nutriments.TryGetValue(key, out var element))
            return null;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var number))
            return number;

        if (element.ValueKind == JsonValueKind.String
            && double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    private static void ApplyDetails(BarcodeProduct entry, ProductLookupDto dto)
    {
        entry.IngredientsText = dto.IngredientsText;
        entry.NutriScoreGrade = dto.NutriScoreGrade;
        entry.EnergyKcal100g = dto.Nutrition?.EnergyKcal100g;
        entry.Fat100g = dto.Nutrition?.Fat100g;
        entry.SaturatedFat100g = dto.Nutrition?.SaturatedFat100g;
        entry.Carbohydrates100g = dto.Nutrition?.Carbohydrates100g;
        entry.Sugars100g = dto.Nutrition?.Sugars100g;
        entry.Fiber100g = dto.Nutrition?.Fiber100g;
        entry.Proteins100g = dto.Nutrition?.Proteins100g;
        entry.Salt100g = dto.Nutrition?.Salt100g;
    }

    private sealed record OffProductResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("product")] OffProduct? Product);

    private sealed record OffProduct(
        [property: JsonPropertyName("product_name")] string? ProductName,
        [property: JsonPropertyName("brands")] string? Brands,
        [property: JsonPropertyName("quantity")] string? Quantity,
        [property: JsonPropertyName("image_url")] string? ImageUrl,
        [property: JsonPropertyName("ingredients_text")] string? IngredientsText,
        [property: JsonPropertyName("nutriments")] Dictionary<string, JsonElement>? Nutriments,
        [property: JsonPropertyName("nutriscore_grade")] string? NutriscoreGrade);

    private static ProductLookupDto ToDto(BarcodeProduct entry)
        => new(
            entry.Barcode,
            entry.Name,
            entry.Brand,
            entry.Quantity,
            entry.ImageUrl,
            entry.IngredientsText,
            entry.NutriScoreGrade,
            ToNutrition(entry));

    private static NutritionFactsDto? ToNutrition(BarcodeProduct entry)
    {
        var facts = new NutritionFactsDto(
            entry.EnergyKcal100g,
            entry.Fat100g,
            entry.SaturatedFat100g,
            entry.Carbohydrates100g,
            entry.Sugars100g,
            entry.Fiber100g,
            entry.Proteins100g,
            entry.Salt100g);

        return facts is
        {
            EnergyKcal100g: null,
            Fat100g: null,
            SaturatedFat100g: null,
            Carbohydrates100g: null,
            Sugars100g: null,
            Fiber100g: null,
            Proteins100g: null,
            Salt100g: null,
        }
            ? null
            : facts;
    }
}
