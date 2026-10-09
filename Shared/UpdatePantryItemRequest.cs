namespace Shared.Models;

public record UpdatePantryItemRequest(
    string Name,
    double Quantity,
    string Unit,
    string Location,
    string? Notes,
    DateOnly ExpirationDate);
