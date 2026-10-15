using System.Net;
using System.Text;

using Server.Services;

namespace Server.Tests;

public class ProductLookupTests
{
    private const string Barcode = "8003590780284";

    private static OpenFoodFactsProductLookupService LookupWithJson(string json, HttpStatusCode status = HttpStatusCode.OK)
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
                "product_name": "Test Biscuits",
                "brands": "TestBrand",
                "quantity": "500 g",
                "nutriscore_grade": "b",
                "image_url": "https://example/img.jpg",
                "ingredients_text": "Wheat flour, sugar, cocoa butter.",
                "nutriments": {
                  "energy-kcal_100g": 450,
                  "proteins_100g": 7.5,
                  "carbohydrates_100g": 60,
                  "sugars_100g": 20,
                  "fat_100g": 18,
                  "saturated-fat_100g": 9,
                  "salt_100g": 0.5,
                  "fiber_100g": 4
                }
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Test Biscuits", result.Name);
        Assert.Equal("TestBrand", result.Brand);
        Assert.Equal(500, result.Quantity);
        Assert.Equal("g", result.Unit);
        Assert.Equal("B", result.NutriScore);
        Assert.Equal("https://example/img.jpg", result.ImageUrl);
        Assert.Equal("Wheat flour, sugar, cocoa butter.", result.IngredientsText);
        Assert.NotNull(result.Nutrition);
        Assert.Equal(450, result.Nutrition.EnergyKcal);
        Assert.Equal(7.5, result.Nutrition.Proteins);
        Assert.Equal(0.5, result.Nutrition.Salt);
        Assert.Equal(4, result.Nutrition.Fiber);
    }

    [Fact]
    public async Task Lookup_Converts_Kj_To_Kcal()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Bar",
                "nutriments": {
                  "energy_100g": 418.4,
                  "energy_unit": "kJ"
                }
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Nutrition);
        Assert.Equal(100, result.Nutrition.EnergyKcal!.Value, precision: 6);
    }

    [Fact]
    public async Task Lookup_Unknown_Energy_Unit_Returns_Null_Energy()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Bar",
                "nutriments": {
                  "energy_100g": 418.4
                }
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.Nutrition?.EnergyKcal);
    }

    [Fact]
    public async Task Lookup_Tolerates_String_Nutriments()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Soup",
                "nutriments": {
                  "energy-kcal_100g": "52,5",
                  "salt_100g": ""
                }
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Nutrition);
        Assert.Equal(52.5, result.Nutrition.EnergyKcal);
        Assert.Null(result.Nutrition.Salt);
        Assert.Null(result.Nutrition.Proteins);
    }

    [Fact]
    public async Task Lookup_Unknown_Barcode_Returns_Null()
    {
        var service = LookupWithJson("""{ "status": 0, "status_verbose": "product not found" }""");

        Assert.Null(await service.LookupAsync(Barcode, CancellationToken.None));
    }

    [Fact]
    public async Task Lookup_NonGrade_NutriScore_Returns_Null()
    {
        var service = LookupWithJson("""
            {
              "status": 1,
              "product": {
                "product_name": "Test Item",
                "nutriscore_grade": "unknown"
              }
            }
            """);

        var result = await service.LookupAsync(Barcode, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.NutriScore);
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
    [InlineData("500 g", 500, "g")]
    [InlineData("1 kg", 1, "kg")]
    [InlineData("250 mg", 250, "mg")]
    [InlineData("1 l", 1, "l")]
    [InlineData("500 ml", 500, "ml")]
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