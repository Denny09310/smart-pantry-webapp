using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Client.Components;

/// <summary>Typed wrapper around the colocated pull-to-refresh JS module.</summary>
public sealed class PullToRefreshInterop(IJSRuntime js) : IAsyncDisposable
{
    internal const string ModulePath = "./Components/Shared/PullToRefresh/PullToRefresh.razor.js";
    internal const string AttachMethod = "attach";
    internal const string DisposeMethod = "dispose";

    private readonly IJSRuntime _js = js;
    private IJSObjectReference? _module;
    private IJSObjectReference? _handle;
    private bool _attached;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async Task AttachAsync(
        ElementReference element,
        DotNetObjectReference<PullToRefresh> dotNet,
        int thresholdPx)
    {
        if (_attached)
            return;

        try
        {
            var module = await GetModuleAsync();
            _handle = await module.InvokeAsync<IJSObjectReference>(AttachMethod, element, dotNet, thresholdPx);
            _attached = true;
        }
        catch (JSDisconnectedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_handle is not null)
            {
                await _handle.InvokeVoidAsync(DisposeMethod);
                await _handle.DisposeAsync();
                _handle = null;
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
                _module = null;
            }
        }
        catch (JSDisconnectedException) { }
    }
}