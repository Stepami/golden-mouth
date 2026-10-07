using Reki.Tests.Generated;

namespace Reki.Tests;

public sealed class TestApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    internal TestApiClient(HttpClient httpClient, long? telegramUserId)
    {
        _httpClient = httpClient;
        if (telegramUserId is { } userId)
        {
            SetAuthenticatedUser(userId);
        }

        Events = new EventsClient(httpClient);
        Player = new PlayerClient(httpClient);
        Posts = new PostsClient(httpClient);
        Social = new SocialClient(httpClient);
        Telemetry = new TelemetryClient(httpClient);
        Worlds = new WorldsClient(httpClient);
    }

    public EventsClient Events { get; }

    public PlayerClient Player { get; }

    public PostsClient Posts { get; }

    public SocialClient Social { get; }

    public TelemetryClient Telemetry { get; }

    public WorldsClient Worlds { get; }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private void SetAuthenticatedUser(long userId)
    {
        _httpClient.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
