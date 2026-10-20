using Client.Models;

using Shared.Models;

namespace Client.Components.Pages;

public partial class Home
{
    /// <summary>All mutable page state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public InfiniteScrollList<CosmeticItemDto>? List { get; set; }
        public int Total { get; set; }
        public CancellationTokenSource? SearchCts { get; set; }

        public string? Category { get; set; } = CosmeticCategories.All;
        public string? Name { get; set; }
        public string Status { get; set; } = CosmeticRules.StatusAll;
        public ItemSort Sort { get; set; } = ItemSort.ExpiryAsc;

        // Evaluated per render: a frozen date drifts expiry badges past midnight
        // on long-lived sessions.
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
    }

    private readonly State _state = new();
}