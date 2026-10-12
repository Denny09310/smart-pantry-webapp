using Bit.Brouter;
using Bit.Butil;
using BlazorBlueprint.Components;
using Client.Components;
using Client.Resources;
using Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddBitButilServices();
builder.Services.AddBitBrouterServices(o =>
{
    o.ViewTransitions = true;
    o.ScrollBehavior = BrouterScrollMode.ToTop;
    o.FocusOnNavigateSelector = "h1";
});
builder.Services.AddServerApi();
builder.Services.AddBottomSheet();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddLocalization();
builder.Services.AddSingleton<Strings>();
builder.Services.AddScoped<LocalizationService>();

var host = builder.Build();

// Initialize localization before running
var locService = host.Services.GetRequiredService<LocalizationService>();
await locService.InitializeAsync();

await host.RunAsync();
