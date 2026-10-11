using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Data.Entities;

using Shared.Models;

namespace Server.Tests;

[Collection("api")]
public sealed class ProductEndpointsTests(PantryApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly StubOffHandler _upstream = factory.Services.GetRequiredService<StubOffHandler>();

    private static HttpResponseMessage OffProduct(string barcode)
    {
        var json = "{\"status\":1,\"code\":\""
            + barcode
            + "\",\"product\":{\"code\":\""
            + barcode
            + "\",\"product_name\":\"Coca-Cola Original\",\"brands\":\"Coca-Cola\",\"quantity\":\"330 ml\",\"image_url\":\"https://images.openfoodfacts.org/x.jpg\""
            + ",\"ingredients_text\":\"Carbonated water, sugar, colour\",\"nutriscore_grade\":\"E\""
            + ",\"nutriments\":{\"energy-kcal_100g\":42,\"fat_100g\":0,\"saturated-fat_100g\":0,\"carbohydrates_100g\":10.6,\"sugars_100g\":10.6,\"fiber_100g\":0,\"proteins_100g\":0,\"salt_100g\":0.01}}}";

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json),
        };
    }

    [Fact]
    public async Task Lookup_Rejects_Invalid_Barcode()
    {
        using var letters = await _client.GetAsync("/api/products/lookup?barcode=abc");
        Assert.Equal(HttpStatusCode.BadRequest, letters.StatusCode);

        using var wrongLength = await _client.GetAsync("/api/products/lookup?barcode=12345");
        Assert.Equal(HttpStatusCode.BadRequest, wrongLength.StatusCode);

        using var missing = await _client.GetAsync("/api/products/lookup");
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task Lookup_Returns_Product_And_Caches()
    {
        // Fresh barcode per run: the shared test database keeps cached rows.
        var barcode = $"299{Random.Shared.Next(100000000, 999999999)}";
        _upstream.Responder = _ => OffProduct(barcode);
        _upstream.RequestedPaths.Clear();

        try
        {
            using var first = await _client.GetAsync($"/api/products/lookup?barcode={barcode}");
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var product = await first.Content.ReadFromJsonAsync<ProductLookupDto>();
            Assert.NotNull(product);
            Assert.Equal(barcode, product.Barcode);
            Assert.Equal("Coca-Cola Original", product.Name);
            Assert.Equal("Coca-Cola", product.Brand);
            Assert.Equal("330 ml", product.Quantity);
            Assert.Equal("Carbonated water, sugar, colour", product.IngredientsText);
            Assert.Equal("e", product.NutriScoreGrade);
            Assert.NotNull(product.Nutrition);
            Assert.Equal(42, product.Nutrition.EnergyKcal100g);
            Assert.Equal(10.6, product.Nutrition.Sugars100g);
            Assert.Equal(0.01, product.Nutrition.Salt100g);
            Assert.Single(_upstream.RequestedPaths);

            // Upstream goes away: the cached row must still resolve.
            _upstream.Responder = _ => throw new HttpRequestException("offline");

            using var second = await _client.GetAsync($"/api/products/lookup?barcode={barcode}");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            var cached = await second.Content.ReadFromJsonAsync<ProductLookupDto>();
            Assert.NotNull(cached);
            Assert.Equal("Coca-Cola Original", cached.Name);
            Assert.Equal("Carbonated water, sugar, colour", cached.IngredientsText);
            Assert.NotNull(cached.Nutrition);
            Assert.Equal(42, cached.Nutrition.EnergyKcal100g);
            Assert.Single(_upstream.RequestedPaths);
        }
        finally
        {
            _upstream.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":0,"status_verbose":"product not found"}"""),
            };
        }
    }

    [Fact]
    public async Task Lookup_Backfills_Details_On_Legacy_Cached_Row()
    {
        // Row cached before details existed: core fields only.
        var barcode = $"299{Random.Shared.Next(100000000, 999999999)}";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.BarcodeProducts.Add(new BarcodeProduct
            {
                Barcode = barcode,
                Name = "Legacy Cola",
            });
            await db.SaveChangesAsync();
        }

        _upstream.Responder = _ => OffProduct(barcode);
        _upstream.RequestedPaths.Clear();

        try
        {
            using var response = await _client.GetAsync($"/api/products/lookup?barcode={barcode}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var product = await response.Content.ReadFromJsonAsync<ProductLookupDto>();
            Assert.NotNull(product);
            Assert.Equal("Coca-Cola Original", product.Name);
            Assert.Equal("Carbonated water, sugar, colour", product.IngredientsText);
            Assert.NotNull(product.Nutrition);
            Assert.Single(_upstream.RequestedPaths);

            // Upstream goes away: the backfilled row resolves fully offline.
            _upstream.Responder = _ => throw new HttpRequestException("offline");

            using var cached = await _client.GetAsync($"/api/products/lookup?barcode={barcode}");
            Assert.Equal(HttpStatusCode.OK, cached.StatusCode);

            var offline = await cached.Content.ReadFromJsonAsync<ProductLookupDto>();
            Assert.NotNull(offline);
            Assert.Equal("Carbonated water, sugar, colour", offline.IngredientsText);
            Assert.Single(_upstream.RequestedPaths);
        }
        finally
        {
            _upstream.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":0,"status_verbose":"product not found"}"""),
            };
        }
    }

    [Fact]
    public async Task Lookup_Unknown_Barcode_Returns_NotFound()
    {
        _upstream.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":0,"status_verbose":"product not found"}"""),
        };

        using var response = await _client.GetAsync("/api/products/lookup?barcode=0000000000000");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Lookup_Upstream_Error_Returns_NotFound()
    {
        _upstream.Responder = _ => throw new HttpRequestException("boom");

        using var response = await _client.GetAsync("/api/products/lookup?barcode=1111111111111");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        _upstream.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":0,"status_verbose":"product not found"}"""),
        };
    }
}