namespace Shared.Models;

public record MemberDto(
    string Id,
    string Name,
    string? Color,
    DateTimeOffset CreatedAt);