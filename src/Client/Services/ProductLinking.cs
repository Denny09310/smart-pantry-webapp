using BlazorBlueprint.Components;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Shared barcode-lookup core for the add/edit form and the details backfill:
/// looks the product up, toasts when unknown or unreachable. Null means the
/// caller should leave everything as-is.
/// </summary>
internal static class ProductLinking
{
    public static async Task<ProductLookupDto?> LookupAsync(
        ServerApi api,
        ToastService toasts,
        ILogger logger,
        LocalizationService loc,
        string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        try
        {
            using var response = await api.Products.LookupAsync(barcode);
            if (response.IsSuccessfulWithContent && response.Content is { } product)
                return product;

            toasts.Info(loc["ProductNotInDatabase"]);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Barcode lookup failed for {Barcode}", barcode);
            toasts.Error(loc["LookupFailed"]);
            return null;
        }
    }
}
