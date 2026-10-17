using BlazorBlueprint.Components;

namespace Client.Services;

/// <summary>
/// Presentation rules for Nutri-Score grades, in one place so the item
/// details badge and the pantry-wide goodness pill can never disagree.
/// </summary>
public static class NutriScoreDisplay
{
    /// <summary>Soft badge colour for a grade letter (A best, E worst).</summary>
    public static BadgeVariant Variant(string? grade) => grade?.Trim().ToUpperInvariant() switch
    {
        "A" => BadgeVariant.SoftSuccess,
        "B" => BadgeVariant.SoftInfo,
        "C" => BadgeVariant.SoftWarning,
        "D" or "E" => BadgeVariant.SoftDestructive,
        _ => BadgeVariant.Outline
    };
}
