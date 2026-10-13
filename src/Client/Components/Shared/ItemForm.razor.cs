using Shared.Models;

namespace Client.Components;

public partial class ItemForm
{
    /// <summary>All mutable form state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public InputModel Input { get; set; } = new();
        public string? EditingId { get; set; }
        public CustomValidator? CustomValidator { get; set; }
        public bool Saving { get; set; }

        public string? ScannedBarcode { get; set; }
        public string? ScannedNutriScore { get; set; }
        public NutritionFacts? ScannedNutrition { get; set; }
        public bool LookingUp { get; set; }
    }

    private readonly State _state = new();
}
