namespace Shared.Models;

public record GetCosmeticItemsResponse(
    IEnumerable<CosmeticItemDto> Items,
    int TotalItems);
