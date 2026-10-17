namespace Shared.Models;

/// <summary>
/// Whole-pantry quick view: how many items sit in each expiry bucket, plus the
/// average Nutri-Score ("goodness") over the items that actually carry one.
/// Averages cover scored items only — <see cref="ScoredItems"/> out of
/// <see cref="Total"/> — so the score is never presented as covering the whole
/// pantry when most items were entered by hand.
/// </summary>
public sealed record PantryStatsDto(
    int Total,
    int Fresh,
    int ExpiringSoon,
    int Expired,
    int ScoredItems,
    double? AverageNutriScore,
    string? NutriScoreGrade);
