namespace Shared.Models;

public record ProductLookupDto(
    string Barcode,
    string Name,
    string? Brand,
    string? Quantity,
    string? ImageUrl);
