namespace Server.Data.Entities;

public class PushSubscription
{
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public string Endpoint { get; set; } = default!;
    public string P256dh { get; set; } = default!;
    public string Auth { get; set; } = default!;

    /// <summary>
    /// BCP-47-ish UI language for push payloads ("en", "it"). The worker
    /// formats each payload in the subscription's language.
    /// </summary>
    public string Language { get; set; } = "en";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}