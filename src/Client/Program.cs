using Bit.Brouter;
using Bit.Butil;

using BlazorBlueprint.Components;

using Client.Components;
using Client.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Shared.Resources;

using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddLocalization();
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
builder.Services.AddScoped<LanguageService>();

var host = builder.Build();

try
{
    var culture = await host.Services.GetRequiredService<LanguageService>().ResolveAsync();

    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;

    ApplyBlueprintStrings(host.Services.GetRequiredService<DefaultBbLocalizer>(), culture);
}
catch (Exception ex)
{
    host.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup")
        .LogWarning(ex, "Culture boot failed.");
}

await host.RunAsync();

static void ApplyBlueprintStrings(DefaultBbLocalizer localizer, CultureInfo culture)
{
    // The library's chrome follows the startup culture; language switches
    // reload the page, so startup configuration is sufficient.
    (string Key, string Resource)[] mappings =
    [
        ("Dialog.Close", nameof(UIStrings.Bb_DialogClose)),
        ("Calendar.GoToPreviousMonth", nameof(UIStrings.Bb_CalendarPrev)),
        ("Calendar.GoToNextMonth", nameof(UIStrings.Bb_CalendarNext)),
        ("DatePicker.Placeholder", nameof(UIStrings.Bb_DatePickerPlaceholder)),
        ("DatePicker.OpenCalendar", nameof(UIStrings.Bb_DatePickerOpen)),
        ("Pagination.Pagination", nameof(UIStrings.Bb_Pagination)),
        ("Pagination.Previous", nameof(UIStrings.Bb_PaginationPrev)),
        ("Pagination.Next", nameof(UIStrings.Bb_PaginationNext)),
        ("Pagination.MorePages", nameof(UIStrings.Bb_PaginationMore)),
        ("Pagination.GoToFirstPage", nameof(UIStrings.Bb_PaginationFirst)),
        ("Pagination.GoToLastPage", nameof(UIStrings.Bb_PaginationLast)),
        ("Pagination.RowsPerPage", nameof(UIStrings.Bb_PaginationRows)),
        ("Pagination.ShowingFormat", nameof(UIStrings.Bb_PaginationShowing)),
        ("Pagination.PageFormat", nameof(UIStrings.Bb_PaginationPage)),
        ("Pagination.NoItems", nameof(UIStrings.Bb_PaginationEmpty)),
        ("DataGrid.Loading", nameof(UIStrings.Bb_GridLoading)),
        ("DataGrid.NoResultsFound", nameof(UIStrings.Bb_GridNoResults)),
    ];

    foreach (var (key, resource) in mappings)
        localizer.Set(key, UIStrings.Get(resource, culture));
}