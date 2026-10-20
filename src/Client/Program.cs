using Bit.Brouter;
using Bit.Butil;

using BlazorBlueprint.Components;

using Client.Components;
using Client.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Http & API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddServerApi();

// UI Libraries
builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddScoped<IBbLocalizer, BlueprintLocalizer>();
builder.Services.AddBitButilServices();

// Routing
builder.Services.AddBitBrouterServices(o =>
{
    o.ViewTransitions = true;
});

// App Services
builder.Services.AddBottomSheet();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<CosmeticRealtimeService>();

// Localization
builder.Services.AddLocalization();
builder.Services.AddScoped<LocalizationService>();

var host = builder.Build();

// Initialize localization before running
var localization = host.Services.GetRequiredService<LocalizationService>();
await localization.InitializeAsync();

await host.RunAsync();