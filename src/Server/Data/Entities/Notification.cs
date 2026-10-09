namespace Server.Data.Entities;

public class Notification
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string PantryItemId { get; set; } = default!;
    public string Message { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}