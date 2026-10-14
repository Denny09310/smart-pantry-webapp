using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Client.Components;

/// <summary>Typed wrapper around the colocated bottom sheet JS module.</summary>
public sealed class BottomSheetInterop(IJSRuntime js) : IAsyncDisposable
{
    internal const string ModulePath = "./Components/Shared/BottomSheet/BottomSheetProvider.razor.js";
    internal const string ShowMethod = "show";
    internal const string CloseMethod = "close";
    internal const string DisposeMethod = "dispose";

    private readonly IJSRuntime _js = js;
    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask ShowAsync(
        ElementReference inner,
        ElementReference handle,
        DotNetObjectReference<BottomSheetProvider> dotNet,
        double[] snapPoints,
        int initialSnapIndex)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(ShowMethod, inner, handle, dotNet, snapPoints, initialSnapIndex);
    }

    public async ValueTask CloseAsync()
    {
        if (_module is null)
            return;

        await _module.InvokeVoidAsync(CloseMethod);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
            {
                await _module.InvokeVoidAsync(DisposeMethod);
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
    }
}