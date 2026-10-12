namespace Shared.Models;

public record GetPantryItemsResponse(
    IEnumerable<PantryItemDto> Items,
    int TotalItems);
