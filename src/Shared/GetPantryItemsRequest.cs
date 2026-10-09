namespace Shared.Models;

public record GetPantryItemsRequest(string? Location, string? Name, ExpiryStatus? Status = null)
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 20;
}
