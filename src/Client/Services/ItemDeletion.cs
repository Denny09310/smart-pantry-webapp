using BlazorBlueprint.Components;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Item deletion always starts by closing the sheet: dialogs render beneath
/// it, so a sheet body never opens a confirm itself. Instead the body closes
/// with the item as the result and the opener confirms and executes here.
/// </summary>
internal static class ItemDeletion
{
    public static async Task HandleAsync(
        DialogResult result,
        ServerApi api,
        DialogService dialogs,
        ToastService toasts,
        ILogger logger,
        LocalizationService loc)
    {
        if (result.Cancelled)
            return;

        if (result.GetData<CosmeticItemDto>() is not { } item)
            return;

        var confirmed = await dialogs.ConfirmAsync(
            loc["DeleteItemConfirm"],
            loc["DeleteItemMessage", item.Name],
            new ConfirmDialogOptions { ConfirmText = loc["Delete"], Destructive = true });

        if (!confirmed.Confirmed)
            return;

        try
        {
            using var response = await api.Cosmetics.DeleteAsync(item.Id);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Item {ItemId} delete failed with {StatusCode}", item.Id, response.StatusCode);
                toasts.Error(loc["CouldNotDelete"]);
                return;
            }

            toasts.Success(loc["ItemDeleted"]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Item {ItemId} delete failed", item.Id);
            toasts.Error(loc["CouldNotDelete"]);
        }
    }
}