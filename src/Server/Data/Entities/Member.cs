namespace Server.Data.Entities;

public class Member
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Name { get; set; } = default!;
    public string? Color { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}