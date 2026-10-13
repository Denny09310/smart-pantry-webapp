using Client.Models;

using Shared.Models;

namespace Client.Components;

public partial class FilterSheetBody
{
    /// <summary>All mutable sheet state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public string? Location { get; set; } = PantryLocations.All;
        public string Status { get; set; } = PantryRules.StatusAll;
        public ItemSort Sort { get; set; } = ItemSort.ExpiryAsc;

        public string? SeenLocation { get; set; } = "__none__";
        public string SeenStatus { get; set; } = "__none__";
        public ItemSort? SeenSort { get; set; }
    }

    private readonly State _state = new();
}
