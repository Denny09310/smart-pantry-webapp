using Shared.Models;

namespace Client.Components;

public partial class ItemDetailsBody
{
    /// <summary>All mutable sheet state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public string LoadedId { get; set; } = string.Empty;
        public bool Loading { get; set; }
        public string? Error { get; set; }
        public PantryItemDto? Item { get; set; }
        public ProductLookupDto? Product { get; set; }
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Today);
        public bool LinkingBarcode { get; set; }
        public bool Unlinking { get; set; }
    }

    private readonly State _state = new();
}