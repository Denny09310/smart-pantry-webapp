using Microsoft.AspNetCore.Generated.Routing;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Hubs;
using Server.Models;
using Server.Services;

using Shared.Validation;

var builder = WebApplication.CreateBuilder(args);

// API & Docs
builder.Services.AddOpenApi();
builder.Services.AddEndpointHandlers();

// Validation
builder.Services.AddValidation();
builder.Services.AddSharedValidation();

// Problem Details & Health
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// SignalR & Presence
builder.Services.AddSignalR();
builder.Services.AddSingleton<PresenceTracker>();

// Caching & Options
builder.Services.AddMemoryCache();
builder.Services.AddVapidOptions(builder.Configuration);

// External HTTP Clients
builder.Services.AddHttpClient<IProductLookupService, OpenFoodFactsProductLookupService>(
    client => client.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddHttpClient<IRecipeService, ThemealDbRecipeService>(
    client => client.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddHttpClient<ITranslationService, MyMemoryTranslationService>(
    client => client.Timeout = TimeSpan.FromSeconds(15));

// Push
builder.Services.AddSingleton<IPushSender, WebPushSender>();

// Background Services
builder.Services.AddHostedService<ReminderWorker>();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

var app = builder.Build();

// Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapEndpointHandlers();
app.MapHub<PantryHub>("/hubs/pantry");

app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new()
{
    Predicate = _ => true,
    ResponseWriter = HealthResponses.WriteJsonAsync
});

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();