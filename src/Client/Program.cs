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

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddBitButilServices();
builder.Services.AddBitBrouterServices(o =>
{
    o.ViewTransitions = true;
});
builder.Services.AddServerApi();
builder.Services.AddBottomSheet();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PantryRealtimeService>();
builder.Services.AddLocalization();
builder.Services.AddScoped<LocalizationService>();

var host = builder.Build();

// Initialize localization before running
var locService = host.Services.GetRequiredService<LocalizationService>();
await locService.InitializeAsync();

await host.RunAsync();