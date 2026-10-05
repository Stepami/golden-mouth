using System.Globalization;
using System.Net;
using Reki.Tests.Generated;
using TgMiniAppAuth;

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
        StoryCards = new StoryCardsClient(httpClient);
        Telemetry = new TelemetryClient(httpClient);
        Worlds = new WorldsClient(httpClient);
    }

    public EventsClient Events { get; }

    public PlayerClient Player { get; }

    public PostsClient Posts { get; }

    public SocialClient Social { get; }

    public StoryCardsClient StoryCards { get; }

    public TelemetryClient Telemetry { get; }

    public WorldsClient Worlds { get; }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private void SetAuthenticatedUser(long userId)
    {
        string serializedUser = $$"""{"id":{{userId}},"first_name":"Test player"}""";
        string rawAuthData = $"user={Uri.EscapeDataString(serializedUser)}";
        var claims = new Dictionary<string, object>
        {
            [TgMiniAppAuthConstants.Claims.Id] = userId.ToString(CultureInfo.InvariantCulture),
            [TgMiniAppAuthConstants.Claims.FirstName] = "Test player",
            [TgMiniAppAuthConstants.Claims.RawAuthData] = rawAuthData,
        };
        _httpClient.SetFakeBearerToken(claims);
    }
}
