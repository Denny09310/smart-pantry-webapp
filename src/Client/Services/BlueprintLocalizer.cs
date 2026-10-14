using BlazorBlueprint.Components;
using Client.Resources;
using Microsoft.Extensions.Localization;

namespace Client.Services;

/// <summary>
/// Bridges BlazorBlueprint component chrome (placeholders, ARIA labels, empty
/// states) to our resx strings. Blueprint keys we don't map fall back to the
/// library's English defaults. Scoped so culture switches apply immediately.
/// </summary>
public sealed class BlueprintLocalizer(IStringLocalizer<Strings> localizer) : DefaultBbLocalizer
{
    private static string? MapKey(string key) => key switch
    {
        "TagInput.RemoveTag" => "BbTagRemove",
        "TagInput.ClearAllTags" => "BbTagClearAll",
        "TagInput.TagSuggestions" => "BbTagSuggestions",
        "DatePicker.OpenCalendar" => "BbDatePickerOpen",
        "Calendar.GoToPreviousMonth" => "BbCalPrevMonth",
        "Calendar.GoToNextMonth" => "BbCalNextMonth",
        "Dialog.Close" => "Close",
        "Sidebar.PillNavigation" => "BbSidebarNav",
        "Sidebar.ExpandNavigation" => "BbSidebarExpand",
        "Sidebar.ToggleSidebar" => "BbSidebarToggle",
        "NotificationBadge.Unread" => "BbNotifUnread",
        "NotificationBadge.Count" => "BbNotifCount",
        "Select.ChooseOption" => "BbSelectChoose",
        _ => null
    };

    public override string this[string key]
        => MapKey(key) is { } mapped && localizer[mapped] is { ResourceNotFound: false } found
            ? found.Value!
            : base[key];

    public override string this[string key, params object[] arguments]
        => MapKey(key) is { } mapped && localizer[mapped, arguments] is { ResourceNotFound: false } found
            ? found.Value!
            : base[key, arguments];
}
