using Bit.Butil;

using Microsoft.AspNetCore.Components;

namespace Client.Components;

public partial class InfiniteScrollList<TItem>
{
    /// <summary>All mutable list state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public List<TItem> Items { get; } = new();
        public int Total { get; set; } = int.MaxValue;
        public bool Loading { get; set; }
        public Task? LoadTask { get; set; }
        public string? Error { get; set; }
        public int Generation { get; set; }
        public bool Disposed { get; set; }

        public ElementReference Sentinel { get; set; }
        public ButilSubscription? Subscription { get; set; }
        public CancellationTokenSource? Cts { get; set; }

        public bool HasMore => Items.Count < Total;
    }

    private readonly State _state = new();
}