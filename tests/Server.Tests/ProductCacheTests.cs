using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Data.Entities;
using Server.Endpoints;
using Server.Services;

using Shared.Models;

namespace Server.Tests;

public class ProductCacheTests(CosmeticApiFactory factory) : IClassFixture<CosmeticApiFactory>
{
    [Fact]
    public async Task Lookup_Miss_Fetches_Remote_And_Caches()
    {
        var barcode = $"800{Guid.CreateVersion7():N}"[..13];
        var remote = new ProductLookupDto(
            barcode, "Test Biscuits", "TestBrand", 500, "g", null, null);
        var lookup = new CountingLookup(remote);

        try
        {
            var first = await LookupAsync(lookup, barcode);
            Assert.Equal("Test Biscuits", first.Name);
            Assert.Equal(1, lookup.Calls);

            // Second lookup uses a fresh scope: a cache hit proves the
            // first call persisted the row, not just tracked it.
            var second = await LookupAsync(lookup, barcode);
            Assert.Equal("Test Biscuits", second.Name);
            Assert.Equal("TestBrand", second.Brand);
            Assert.Equal(500, second.Quantity);
            Assert.Equal(1, lookup.Calls);
        }
        finally
        {
            await DeleteCachedAsync(barcode);
        }
    }

    [Fact]
    public async Task Lookup_Unknown_Remote_Falls_Back_To_Local_Items()
    {
        var barcode = $"801{Guid.CreateVersion7():N}"[..13];
        var name = $"test-cache-item-{Guid.CreateVersion7()}";
        string id;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var item = new CosmeticItem
            {
                Name = name,
                Quantity = 2,
                Unit = "pcs",
                Category = "face",
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                Barcode = barcode
            };
            db.Items.Add(item);
            await db.SaveChangesAsync();
            id = item.Id;
        }

        try
        {
            var result = await LookupAsync(new CountingLookup(null), barcode);
            Assert.Equal(name, result.Name);
            Assert.Equal(2, result.Quantity);
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Items.Where(x => x.Id == id).ExecuteDeleteAsync();
        }
    }

    private async Task<ProductLookupDto> LookupAsync(CountingLookup lookup, string barcode)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var endpoints = new ProductEndpoints(db, lookup);

        var response = await endpoints.LookupAsync(
            new LookupProductRequest(barcode), CancellationToken.None);

        var ok = Assert.IsType<Ok<ProductLookupDto>>(response.Result);
        Assert.NotNull(ok.Value);
        return ok.Value;
    }

    private async Task DeleteCachedAsync(string barcode)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.CachedProducts.Where(x => x.Barcode == barcode).ExecuteDeleteAsync();
    }

    private sealed class CountingLookup(ProductLookupDto? result) : IProductLookupService
    {
        public int Calls;

        public Task<ProductLookupDto?> LookupAsync(string barcode, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }
}