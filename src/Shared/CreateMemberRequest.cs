using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Validation;

namespace Shared.Models;

[ValidatableType]
public record CreateMemberRequest(
    [Required]
    [StringLength(30)]
    string Name,

    string? Color);