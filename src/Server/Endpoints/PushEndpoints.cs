using Microsoft.AspNetCore.Generated.Attributes;
using Shared.Models;

namespace Server.Endpoints;

[Tags("Push")]
[MapGroup("/api/push")]
internal static class PushEndpoints
{
    [MapGet("/public-key")]
    public static GetPushNotificationKey GetPushNotificationKeyAsync(
        IConfiguration configuration) => new(configuration["Vapid:PublicKey"]!);
}

