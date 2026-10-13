using Shared.Models;

namespace Client.Components;

public partial class ItemDetailsBody
{
    /// <summary>All mutable sheet state in one place; nothing here is a parameter.</summary>
    private sealed class State
    {
        public PantryItemDto? Item { get; set; }
        public ProductLookupDto? Product { get; set; }
        public string? LoadedId { get; set; }
        public bool Loading { get; set; } = true;
        public string? Error { get; set; }
        public bool LinkingBarcode { get; set; }
        public bool Unlinking { get; set; }

        public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Today);
    }

    private readonly State _state = new();
}
