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

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddBitBrouterServices(o =>
{
    o.ViewTransitions = true;
    o.FocusOnNavigateSelector = "h1";
});
builder.Services.AddBlazorBlueprintComponents();
builder.Services.AddBitButilServices();
builder.Services.AddBottomSheet();
builder.Services.AddServerApi();
builder.Services.AddScoped<MemberService>();

await builder.Build().RunAsync();