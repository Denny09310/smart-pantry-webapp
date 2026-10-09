using Microsoft.AspNetCore.Generated.Routing;
using Microsoft.EntityFrameworkCore;

using Server.Data;
using Server.Extensions;
using Server.Services;
using Server.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointHandlers();

builder.Services.AddValidation();
builder.Services.AddSharedValidation();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!);

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseNpgsql(
        // Resolved lazily: test-host configuration applies after Program runs.
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PushService>();
builder.Services.AddScoped<ProductLookupService>();
builder.Services.AddOpenFoodFacts();
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.SectionName));

if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<ExpirationCheckWorker>();

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Default")))
        throw new InvalidOperationException("Missing ConnectionStrings:Default. Set it via appsettings, user secrets, or the ConnectionStrings__Default environment variable.");

    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration failed. Check ConnectionStrings:Default and that Postgres is reachable.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
}

app.MapHealthChecks("/healthz");

app.UseHttpsRedirection();

app.UseServiceWorkerNoCache();

app.MapEndpointHandlers();

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();