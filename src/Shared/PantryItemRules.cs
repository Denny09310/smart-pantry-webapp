using System.ComponentModel.DataAnnotations;

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
        DateOnly expirationDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return new ValidationResult("Name is required.", ["Name"]);
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            yield return new ValidationResult("Unit is required.", ["Unit"]);
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            yield return new ValidationResult("Location is required.", ["Location"]);
        }

        if (expirationDate == default)
        {
            yield return new ValidationResult("Expiration date is required.", ["ExpirationDate"]);
        }
    }
}
