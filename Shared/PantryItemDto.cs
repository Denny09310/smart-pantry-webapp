namespace Shared.Models;

public record PantryItemDto(
    string Id,
    string Name,
    double Quantity,
    string Unit,
    string Location,
    string? Notes,
    DateOnly ExpirationDate,
    ExpiryStatus Status);
