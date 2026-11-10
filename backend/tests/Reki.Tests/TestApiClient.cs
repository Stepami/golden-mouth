using System.Net;
using System.Net.Http.Headers;
using System.Text;
using MongoDB.Driver;
using OpenIddict.Abstractions;
using Reki.Api.Requests.Player;
using Reki.Application.Player.Responses;
using Reki.Domain.Player;

namespace Reki.Tests;

public sealed class TestApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ApiFactory _factory;
    private readonly long? _telegramId;

    internal TestApiClient(HttpClient httpClient, PlayerId? playerId, long? telegramId, ApiFactory factory)
    {
        _httpClient = httpClient;
        _factory = factory;
        _telegramId = telegramId;
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

    public async Task<PlayerResponse> RegisterAsync(OnboardingRequest request, CancellationToken cancellationToken)
    {
        var telegramId = _telegramId ?? throw new InvalidOperationException("Telegram identity is required.");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(ExternalUserId.Telegram(telegramId).Value)));
        await new ExternalTelegramAuthClient(client).CompleteOnboardingAsync(request, cancellationToken);
        var externalId = ExternalUserId.Telegram(telegramId);
        var player = await _factory.Session.Database.GetCollection<PlayerProfile>("players")
            .Find(item => item.ExternalUserIds.Contains(externalId)).SingleAsync(cancellationToken);
        SetAuthenticatedUser(player.Id);
        return await Player.GetCurrentPlayerAsync(cancellationToken);
    }

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
