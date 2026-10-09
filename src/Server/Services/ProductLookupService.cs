using System.Net.Http.Json;
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
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

        if (cached is not null)
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
            return null;

        db.BarcodeProducts.Add(new BarcodeProduct
        {
            Barcode = fresh.Barcode,
            Name = fresh.Name,
            Brand = fresh.Brand,
            Quantity = fresh.Quantity,
            ImageUrl = fresh.ImageUrl,
        });

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
            $"api/v2/product/{barcode}.json?fields=code,product_name,brands,quantity,image_url",
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
            product.ImageUrl);
    }

    private sealed record OffProductResponse(
        [property: JsonPropertyName("status")] int Status,
        [property: JsonPropertyName("product")] OffProduct? Product);

    private sealed record OffProduct(
        [property: JsonPropertyName("product_name")] string? ProductName,
        [property: JsonPropertyName("brands")] string? Brands,
        [property: JsonPropertyName("quantity")] string? Quantity,
        [property: JsonPropertyName("image_url")] string? ImageUrl);

    private static ProductLookupDto ToDto(BarcodeProduct entry)
        => new(
            entry.Barcode,
            entry.Name,
            entry.Brand,
            entry.Quantity,
            entry.ImageUrl);
}
