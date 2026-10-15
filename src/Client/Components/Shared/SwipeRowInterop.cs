using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Client.Components;

/// <summary>Typed wrapper around the colocated swipe-row gesture module.</summary>
public sealed class SwipeRowInterop(IJSRuntime js) : IAsyncDisposable
{
    // Bump the version query whenever this module changes: dynamic import()
    // responses are HTTP-cached without a content hash, so devices otherwise
    // keep running the previous module.
    internal const string ModulePath = "./Components/Shared/SwipeableRow.razor.js?v=1";

    private readonly IJSRuntime _js = js;
    private IJSObjectReference? _module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask AttachAsync(ElementReference content, double actionsWidth)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("attach", content, actionsWidth);
    }

    public async ValueTask UpdateAsync(ElementReference content, double actionsWidth)
    {
        if (_module is null)
            return;

        await _module.InvokeVoidAsync("update", content, actionsWidth);
    }

    public async ValueTask ResetAsync(ElementReference content)
    {
        if (_module is null)
            return;

        await _module.InvokeVoidAsync("reset", content);
    }

    public async ValueTask DetachAsync(ElementReference content)
    {
        if (_module is null)
            return;

        try
        {
            await _module.InvokeVoidAsync("detach", content);
        }
        catch (JSDisconnectedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
                await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }
}
