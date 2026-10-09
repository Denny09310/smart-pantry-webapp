using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

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
            + "\",\"product_name\":\"Coca-Cola Original\",\"brands\":\"Coca-Cola\",\"quantity\":\"330 ml\",\"image_url\":\"https://images.openfoodfacts.org/x.jpg\"}}";

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
            Assert.Single(_upstream.RequestedPaths);

            // Upstream goes away: the cached row must still resolve.
            _upstream.Responder = _ => throw new HttpRequestException("offline");

            using var second = await _client.GetAsync($"/api/products/lookup?barcode={barcode}");
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            var cached = await second.Content.ReadFromJsonAsync<ProductLookupDto>();
            Assert.NotNull(cached);
            Assert.Equal("Coca-Cola Original", cached.Name);
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
