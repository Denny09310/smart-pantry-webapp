using Bit.Butil;

namespace Client.Services;

/// <summary>
/// Scrolls a freshly created pantry row into view. Fire-and-forget safe:
/// failures (prerendering, disconnected circuit) are swallowed.
/// </summary>
internal static class PantryScroll
{
    public static async Task ScrollToAsync(Dom dom, string itemId)
    {
        try
        {
            await using var handle = await dom.ById($"pantry-item-{itemId}");

            if (handle is null)
                return;

            var reference = await handle.AsElementReference();

            if (reference is null)
                return;

            await reference.Value.ScrollIntoView(new ScrollIntoViewOptions
            {
                Behavior = ScrollBehavior.Smooth,
                Block = ScrollLogicalPosition.Nearest,
            });
        }
        catch (Exception)
        {
            // Best effort: prerendering, navigation, or a disconnected circuit.
        }
    }
}
