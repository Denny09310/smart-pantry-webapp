using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Localization;

using Shared.Resources;

namespace Shared.Models;

/// <summary>
/// Rules attributes can't express (whitespace-only text passes [Required];
/// non-nullable value types are always "present"). Shared by the pantry item
/// request records so behavior stays identical.
/// </summary>
internal static class PantryItemRules
{
    public static IEnumerable<ValidationResult> Validate(
        string? name,
        string? unit,
        string? location,
        DateOnly expirationDate,
        IStringLocalizer<UIStrings>? localizer)
    {
        string Text(string key) => localizer?.GetString(key) ?? UIStrings.Get(key);

        if (string.IsNullOrWhiteSpace(name))
        {
            yield return new ValidationResult(Text(nameof(UIStrings.V_ItemNameRequired)), ["Name"]);
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            yield return new ValidationResult(Text(nameof(UIStrings.V_UnitRequired)), ["Unit"]);
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            yield return new ValidationResult(Text(nameof(UIStrings.V_LocationRequired)), ["Location"]);
        }

        if (expirationDate == default)
        {
            yield return new ValidationResult(Text(nameof(UIStrings.V_ExpirationRequired)), ["ExpirationDate"]);
        }
    }
}
