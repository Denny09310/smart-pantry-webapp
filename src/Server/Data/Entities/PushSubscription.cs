namespace Server.Data.Entities;

public class PushSubscription
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
}
