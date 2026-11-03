namespace Reki.Api.Auth;

internal sealed class TelegramMetadataProxyHandler(Uri proxyBase) : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is { Host: "oauth.telegram.org" } uri)
        {
            request.RequestUri = new Uri(proxyBase, uri.PathAndQuery.TrimStart('/'));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
