using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public record CreateMemberRequest(
    [Required]
    [StringLength(30)]
    string Name,

    string? Color);