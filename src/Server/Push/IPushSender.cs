namespace Server.Push;

public enum PushSendOutcome
{
    Delivered,
    InvalidSubscription,
    Failed,
    NotConfigured
}

public sealed record WebPushSenderRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    string Title,
    string Body,
    string Url);

public interface IPushSender
{
    Task<PushSendOutcome> TrySendAsync(WebPushSenderRequest request, CancellationToken ct);
}
