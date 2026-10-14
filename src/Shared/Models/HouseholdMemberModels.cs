using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation.Embedded;

namespace Shared.Models;

public record HouseholdMemberDto(
    string Id,
    string DisplayName,
    string? Color,
    bool IsArchived);

[ValidatableType]
public record CreateHouseholdMemberRequest(
    [property: Required, MaxLength(100)] string DisplayName,
    [property: MaxLength(16)] string? Color = null);

[ValidatableType]
public record UpdateHouseholdMemberRequest(
    [property: Required, MaxLength(100)] string DisplayName,
    [property: MaxLength(16)] string? Color,
    bool IsArchived);