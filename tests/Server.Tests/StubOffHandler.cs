using System.Net;

namespace Server.Tests;

/// <summary>
/// Programmable upstream for Open Food Facts: tests set <see cref="Responder"/>
/// to control what the lookup service sees. Defaults to product-not-found.
/// </summary>
public sealed class StubOffHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
        _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"status":0,"status_verbose":"product not found"}"""),
        };

    public List<string> RequestedPaths { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
        return Task.FromResult(Responder(request));
    }
}

public sealed class StubHttpClientFactory(StubOffHandler stub) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(stub, disposeHandler: false)
    {
        BaseAddress = new Uri("https://world.openfoodfacts.org/"),
    };
}
