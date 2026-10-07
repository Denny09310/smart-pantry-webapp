using Microsoft.AspNetCore.Generated.Attributes;

namespace Server.Endpoints;

[Tags("Greetings")]
internal static class GreetingEndpoints
{
    [MapGet("/api/greetings")]
    public static string GetGreetings() => "Hello, world";
}
