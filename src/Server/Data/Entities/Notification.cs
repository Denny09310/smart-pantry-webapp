namespace Server.Data.Entities;

public class Notification
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();

    /// <summary>Reminder identity part: e.g. "expiry-warning".</summary>
    public string Type { get; set; } = default!;

    public string PantryItemId { get; set; } = default!;
    public PantryItem? PantryItem { get; set; }

    /// <summary>Expiry date the reminder was created for (dedup identity part).</summary>
    public DateOnly ExpirationDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
