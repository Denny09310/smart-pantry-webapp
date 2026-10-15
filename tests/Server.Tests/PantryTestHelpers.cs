using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Server.Data;
using Server.Data.Entities;

namespace Server.Tests;

/// <summary>Shared EF seed/cleanup helpers (one database per test class).</summary>
internal static class PantryTestHelpers
{
    public static async Task<PantryItem> SeedItemAsync(
        IServiceProvider services, string name, DateOnly? expiration = null)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var item = new PantryItem
        {
            Name = name,
            Quantity = 1,
            Unit = "pcs",
            Location = "pantry",
            ExpirationDate = expiration ?? DateOnly.FromDateTime(DateTime.Today.AddDays(2))
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public static async Task DeleteItemAsync(IServiceProvider services, string id)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Id == id).ExecuteDeleteAsync();
    }

    public static async Task DeleteItemsByNameAsync(IServiceProvider services, string name)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Items.Where(x => x.Name == name).ExecuteDeleteAsync();
    }

    public static async Task DeleteMemberAsync(IServiceProvider services, string id)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Members.Where(x => x.Id == id).ExecuteDeleteAsync();
    }

    public static async Task DeleteMembersByNameAsync(IServiceProvider services, string name)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Members.Where(x => x.DisplayName == name).ExecuteDeleteAsync();
    }

    public static async Task SeedSubscriptionAsync(IServiceProvider services, string endpoint)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.PushSubscriptions.Add(new PushSubscription
        {
            Endpoint = endpoint,
            P256dh = "p256dh",
            Auth = "auth"
        });
        await db.SaveChangesAsync();
    }

    public static async Task DeleteSubscriptionAsync(IServiceProvider services, string endpoint)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.PushSubscriptions.Where(x => x.Endpoint == endpoint).ExecuteDeleteAsync();
    }
}
