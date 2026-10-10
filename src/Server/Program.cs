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
builder.Services.AddLocalization();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
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

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();