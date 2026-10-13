using Client.Models;

using Shared.Models;

namespace Client.Components.Pages;

public partial class Home
{
    /// <summary>All mutable page state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public InfiniteScrollList<PantryItemDto>? List { get; set; }
        public int Total { get; set; }
        public CancellationTokenSource? SearchCts { get; set; }

        public string? Location { get; set; } = PantryLocations.All;
        public string? Name { get; set; }
        public string Status { get; set; } = PantryRules.StatusAll;
        public ItemSort Sort { get; set; } = ItemSort.ExpiryAsc;

        public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);
    }

    private readonly State _state = new();
}
