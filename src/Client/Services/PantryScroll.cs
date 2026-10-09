using Microsoft.JSInterop;

namespace Client.Services;

/// <summary>
/// Scrolls a freshly created pantry row into view. Fire-and-forget safe:
/// failures (prerendering, disconnected circuit) are swallowed.
/// </summary>
internal static class PantryScroll
{
    public static async Task ScrollToAsync(IJSRuntime js, string itemId)
    {
        try
        {
            await using var module = await js.InvokeAsync<IJSObjectReference>("import", "/js/scroll-helper.js");
            await module.InvokeVoidAsync("scrollToItem", itemId);
        }
        catch (Exception)
        {
            // Best effort: prerendering, navigation, or a disconnected circuit.
        }
    }
}
