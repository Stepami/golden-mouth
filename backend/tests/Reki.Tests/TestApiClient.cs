using System.Net;
using OpenIddict.Abstractions;
using Reki.Domain.Player;

namespace Reki.Tests;

public sealed class TestApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    internal TestApiClient(HttpClient httpClient, PlayerId? playerId)
    {
        _httpClient = httpClient;
        if (playerId is { } id)
        {
            SetAuthenticatedUser(id);
        }

        StoryCards = new StoryCardsClient(httpClient);
        Events = new EventsClient(httpClient);
        Player = new PlayerClient(httpClient);
        Posts = new PostsClient(httpClient);
        Social = new SocialClient(httpClient);
        Telemetry = new TelemetryClient(httpClient);
        Worlds = new WorldsClient(httpClient);
    }

    public StoryCardsClient StoryCards { get; }

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

    private void SetAuthenticatedUser(PlayerId playerId)
    {
        _httpClient.SetFakeJwtBearerToken(new Dictionary<string, object>
        {
            [OpenIddictConstants.Claims.Subject] = playerId.ToString(),
        });
    }
}
