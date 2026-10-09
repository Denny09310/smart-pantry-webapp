namespace Shared.Models;

public record PushSubscriptionRequest(
    string Endpoint,
    string P256dh,
    string Auth);
