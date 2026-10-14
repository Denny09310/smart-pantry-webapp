using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Client.Components;

public partial class PullToRefresh
{
    /// <summary>All mutable component state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public ElementReference Ref { get; set; }
        public PullToRefreshInterop? Interop { get; set; }
        public DotNetObjectReference<PullToRefresh>? DotNetRef { get; set; }
        public bool Refreshing { get; set; }
        public bool Disposed { get; set; }
    }

    private readonly State _state = new();
}
