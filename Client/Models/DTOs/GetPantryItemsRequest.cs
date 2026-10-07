namespace Shared.Models;

public record GetPantryItemsRequest(string? Location, string? Name)
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 20;
}
