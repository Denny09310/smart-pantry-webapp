namespace Shared.Models;

public record CreatePantryItemRequest(
    string Name,
    double Quantity,
    string Unit,
    string Location,
    string? Notes,
    DateOnly ExpirationDate);
