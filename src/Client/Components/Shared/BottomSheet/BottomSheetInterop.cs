using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Client.Components.Shared;

/// <summary>Options passed to the colocated JS module in a single interop call.</summary>
public sealed class BottomSheetJsOptions
{
    public double[] SnapPoints { get; init; } = [0.5, 0.9];

    public int InitialSnap { get; init; }

    public bool Dismissible { get; init; } = true;

    public bool OverlayDismiss { get; init; } = true;

    public bool EscapeDismiss { get; init; } = true;
}

/// <summary>Typed wrapper around the colocated BottomSheet JS module.</summary>
public sealed class BottomSheetInterop(IJSRuntime js) : IAsyncDisposable
{
    internal const string ModulePath = "./Components/Shared/BottomSheet/BottomSheet.razor.js";
    internal const string AttachMethod = "attach";
    internal const string DismissMethod = "dismiss";
    internal const string DetachMethod = "detach";
    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask AttachAsync(
        string id,
        ElementReference sheet,
        ElementReference overlay,
        ElementReference handle,
        DotNetObjectReference<BottomSheet> dotNetRef,
        BottomSheetJsOptions options)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(AttachMethod, id, sheet, overlay, handle, dotNetRef, options);
    }

    public async ValueTask DismissAsync(string id)
    {
        if (_module is null)
            return;

        await _module.InvokeVoidAsync(DismissMethod, id);
    }

    public async ValueTask DetachAsync(string id)
    {
        if (_module is null)
            return;

        await _module.InvokeVoidAsync(DetachMethod, id);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
                await _module.DisposeAsync();
        }
        catch (Exception)
        {
            // Teardown during navigation: module may already be gone.
        }

        _module = null;
    }
}