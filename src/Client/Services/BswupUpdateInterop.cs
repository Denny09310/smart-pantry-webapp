using Microsoft.JSInterop;

namespace Client.Services;

/// <summary>
/// Typed interop for the Bswup update notifier: registers the global Bswup
/// lifecycle handler from Blazor and activates staged updates on accept.
/// </summary>
public sealed class BswupUpdateInterop : IAsyncDisposable
{
    internal const string ModulePath = "./Components/Shared/UpdateNotifier.razor.js";
    internal const string RegisterMethod = "registerBswupHandler";
    internal const string ActivateMethod = "activateUpdate";
    internal const string ForceReloadMethod = "forceReload";
    internal const string DisposeMethod = "disposeBswupHandler";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public BswupUpdateInterop(IJSRuntime js) => _js = js;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);

    public async ValueTask RegisterAsync<T>(DotNetObjectReference<T> dotNetRef) where T : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(RegisterMethod, dotNetRef);
    }

    public async ValueTask ActivateAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(ActivateMethod);
    }

    public async ValueTask ForceReloadAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(ForceReloadMethod);
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
        catch (Exception)
        {
            // Teardown during navigation: module may already be gone.
        }
    }
}