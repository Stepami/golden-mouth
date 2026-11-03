namespace Reki.Api.Auth;

internal sealed class TelegramBotApiProxyHandler(Uri workerApiBase) : DelegatingHandler(
    new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(3) })
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sdkUri = request.RequestUri ?? throw new InvalidOperationException("Telegram Bot API request URI is missing.");

        // The dot keeps the colon in bot<ID>:<token> inside a relative path.
        request.RequestUri = new Uri(workerApiBase, "." + sdkUri.PathAndQuery);
        return base.SendAsync(request, cancellationToken);
    }
}
