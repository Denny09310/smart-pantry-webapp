using System.Net;
using System.Text;

using Server.Services;

namespace Server.Tests;

public class ProductLookupTests
{
    private const string Barcode = "8003590780284";

    private static OpenBeautyFactsProductLookupService LookupWithJson(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(new HttpClient(new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        })));

    [Fact]
    public async Task Lookup_Maps_Snake_Case_Payload()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Cream",
                "brands": "TestBrand",
                "quantity": "500 ml",
                "image_url": "https://example/img.jpg",
                "ingredients_text": "Aqua, glycerin, cetyl alcohol."
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Test Cream", result.Name);
        Assert.Equal("TestBrand", result.Brand);
        Assert.Equal(500, result.Quantity);
        Assert.Equal("ml", result.Unit);
        Assert.Equal("https://example/img.jpg", result.ImageUrl);
        Assert.Equal("Aqua, glycerin, cetyl alcohol.", result.IngredientsText);
    }

    [Fact]
    public async Task Lookup_Unknown_Barcode_Returns_Null()
    {
        var service = LookupWithJson("""{ "status": 0, "status_verbose": "product not found" }""");

        Assert.Null(await service.LookupAsync(Barcode, CancellationToken.None));
    }

    [Fact]
    public async Task Lookup_Overlong_Name_Is_Truncated()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "»»»LONG«««"
              }
            }
            """.Replace("»»»LONG«««", new string('x', 250)));

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(200, result.Name.Length);
    }

    [Fact]
    public async Task Lookup_Dedupes_Repeated_Brands()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Milk",
                "brands": "Acme, acme, ACME , , FreshFarm"
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Acme, FreshFarm", result.Brand);
    }

    [Theory]
    [InlineData("500 ml", 500, "ml")]
    [InlineData("250 mg", 250, "mg")]
    [InlineData("1 l", 1, "l")]
    [InlineData("25 cl", 25, "cl")]
    [InlineData("16 oz", 16, "oz")]
    [InlineData("1 lb", 1, "lb")]
    public async Task Lookup_Parses_Units_Longest_First(string quantity, double value, string unit)
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Item",
                "quantity": "QUANTITY"
              }
            }
            """.Replace("QUANTITY", quantity));

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(value, result.Quantity);
        Assert.Equal(unit, result.Unit);
    }

    [Fact]
    public async Task Lookup_Http_Failure_Returns_Null()
    {
        var service = LookupWithJson("{}", HttpStatusCode.InternalServerError);

        Assert.Null(await service.LookupAsync(Barcode, CancellationToken.None));
    }

}
