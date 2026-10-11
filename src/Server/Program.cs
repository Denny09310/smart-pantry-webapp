using Microsoft.AspNetCore.Generated.Routing;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Extensions;
using Server.Hubs;
using Server.Services;
using Server.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointHandlers();

builder.Services.AddSharedValidation();
builder.Services.AddLocalization();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "Missing ConnectionStrings:Default. Set it via user-secrets or environment (see appsettings.json)."));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PushService>();
builder.Services.AddScoped<ProductLookupService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<TranslationService>();
builder.Services.AddOpenFoodFacts();
builder.Services.AddTheMealDb();
builder.Services.AddMyMemory();
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.SectionName));

if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<ExpirationCheckWorker>();

var app = builder.Build();

app.UseExceptionHandler();

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

app.MapHealthChecks("/healthz");

app.UseHttpsRedirection();

// Validation messages follow the request's Accept-Language.
app.UseRequestLocalization(options =>
{
    options.AddSupportedCultures("en", "it");
    options.AddSupportedUICultures("en", "it");
    options.SetDefaultCulture("en");
});

app.MapEndpointHandlers();

app.MapHub<PantryHub>("/hubs/pantry");

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();