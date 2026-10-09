using Microsoft.AspNetCore.Generated.Routing;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Extensions;
using Server.Services;
using Server.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointHandlers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PushService>();
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection(PushOptions.SectionName));

// The daily check has no business running inside the test host:
// it would race the tests against the shared database.
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddHostedService<ExpirationCheckWorker>();

var app = builder.Build();

// Keep a sample database usable without manual tooling.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseServiceWorkerNoCache();

app.MapEndpointHandlers();

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();