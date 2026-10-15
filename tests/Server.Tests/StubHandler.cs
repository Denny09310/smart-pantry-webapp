namespace Server.Tests;

/// <summary>One-shot <see cref="HttpMessageHandler"/> fake for service unit tests.</summary>
internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(respond(request));
}
