namespace Server.Data.Entities;

public class HouseholdMember
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string DisplayName { get; set; } = default!;
    public string? Color { get; set; }

    public bool IsArchived { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}