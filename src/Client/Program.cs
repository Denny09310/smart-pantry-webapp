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
    o.ScrollBehavior = BrouterScrollMode.ToTop;
    o.FocusOnNavigateSelector = "h1";
});
builder.Services.AddServerApi();
builder.Services.AddScoped<BottomSheetService>();
builder.Services.AddScoped<MemberService>();

await builder.Build().RunAsync();
