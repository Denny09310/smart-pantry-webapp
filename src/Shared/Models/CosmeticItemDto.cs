namespace Shared.Models;

public record CosmeticItemDto(
    string Id,
    string Name,
    double Quantity,
    string Unit,
    string Category,
    string? Notes,
    DateOnly ExpiryDate,
    string? HouseholdMemberId,
    string? Barcode);
