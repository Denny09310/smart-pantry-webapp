namespace Server.Data.Entities;

/// <summary>
/// A browser/device push subscription. Belongs to a device, never to a
/// member identity; reminders are broadcast to every stored subscription.
/// </summary>
public class PushSubscription
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();

    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
