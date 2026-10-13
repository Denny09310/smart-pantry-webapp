using Microsoft.AspNetCore.Generated.Routing;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Hubs;
using Server.Models;
using Server.Services;
using Shared.Validation;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointHandlers();

builder.Services.AddValidation();
builder.Services.AddSharedValidation();

builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddVapidOptions(builder.Configuration);
builder.Services.AddSingleton<IPushSender, WebPushSender>();
builder.Services.AddHostedService<ReminderWorker>();

builder.Services.AddHttpClient<IProductLookupService, OpenFoodFactsProductLookupService>(
    client => client.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddHttpClient<IRecipeService, ThemealDbRecipeService>(
    client => client.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddHttpClient<ITranslationService, MyMemoryTranslationService>(
    client => client.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapEndpointHandlers();

app.MapHub<PantryHub>("/hubs/pantry");

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();