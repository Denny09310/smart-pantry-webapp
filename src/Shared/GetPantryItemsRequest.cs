namespace Shared.Models;

public record GetPantryItemsRequest(string? Location, string? Name, ExpiryStatus? Status = null)
{
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 20;
    public bool ExcludeFresh { get; init; } = false;
    public bool SortDescending { get; init; } = false;
}
