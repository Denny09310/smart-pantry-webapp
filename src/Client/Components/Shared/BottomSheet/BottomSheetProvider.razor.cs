using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Client.Services;

namespace Client.Components;

public partial class BottomSheetProvider
{
    /// <summary>All mutable UI state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public VisibleBody? Body { get; set; }
        public SheetOptions Options { get; set; } = SheetOptions.Default;
        public ElementReference InnerRef { get; set; }
        public ElementReference HandleRef { get; set; }

        public BottomSheetInterop? Interop { get; set; }
        public DotNetObjectReference<BottomSheetProvider>? DotNetRef { get; set; }
        public bool PendingShow { get; set; }
        public bool Closing { get; set; }
        public bool GestureClosed { get; set; }
    }

    private readonly State _state = new();
}
