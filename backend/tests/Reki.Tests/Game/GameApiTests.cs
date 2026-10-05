using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Reki.Tests.Game;

[NotInParallel]
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class GameApiTests(ApiFactory factory)
{
    private IMongoDatabase Database => new MongoClient(factory.MongoConnectionString)
        .GetDatabase(factory.DatabaseName);

    [Test]
    public async Task Me_WithoutTelegramAuthentication_ReturnsUnauthorizedAsync()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Onboarding_WithEnumContracts_PersistsAgeGroupWithoutBirthYearAsync()
    {
        const long telegramUserId = 1001;
        using HttpClient client = CreateAuthenticatedClient(telegramUserId);

        using HttpResponseMessage response = await OnboardAsync(client);
        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(document.RootElement.GetProperty("postsCap").GetInt32()).IsEqualTo(10);
        await Assert.That(document.RootElement.GetProperty("postsLeft").GetInt32()).IsEqualTo(10);
        await Assert.That(document.RootElement.GetProperty("world").GetString()).IsEqualTo("stage");

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        await Assert.That(player["BirthGroup"].AsString).IsEqualTo("14-17");
        await Assert.That(player.Names.Any(name => name.Contains("year", StringComparison.OrdinalIgnoreCase)))
            .IsFalse();
    }

    [Test]
    public async Task PublishPost_AfterDailyLimit_ReturnsConflictAsync()
    {
        using HttpClient client = CreateAuthenticatedClient(1002);
        using HttpResponseMessage onboarding = await OnboardAsync(client);
        onboarding.EnsureSuccessStatusCode();

        for (int postIndex = 0; postIndex < 10; postIndex++)
        {
            using HttpResponseMessage published = await PostJsonAsync(
                client,
                "/posts",
                $$"""{"text":"post {{postIndex}}"}""");
            await published.Content.ReadAsStringAsync();
            await Assert.That(published.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using HttpResponseMessage response = await PostJsonAsync(
            client,
            "/posts",
            """{"text":"over limit"}""");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Settings_WhenPlusIsEnabled_UsesPlusDailyLimitAsync()
    {
        using HttpClient client = CreateAuthenticatedClient(1005);
        using HttpResponseMessage onboarding = await OnboardAsync(client);
        onboarding.EnsureSuccessStatusCode();
        using var content = new StringContent("""{"plus":true}""", Encoding.UTF8, "application/json");

        using HttpResponseMessage settings = await client.PatchAsync("/settings", content);
        using HttpResponseMessage response = await client.GetAsync("/me");
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        await Assert.That(settings.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(document.RootElement.GetProperty("postsCap").GetInt32()).IsEqualTo(20);
        await Assert.That(document.RootElement.GetProperty("postsLeft").GetInt32()).IsEqualTo(20);
    }

    [Test]
    public async Task ClientLogs_InDevelopment_AreNotWrittenToMongoAsync()
    {
        using HttpClient client = CreateAuthenticatedClient(1003);
        using HttpResponseMessage onboarding = await OnboardAsync(client);
        onboarding.EnsureSuccessStatusCode();

        const string requestJson = """
            {
              "session": "test-session",
              "entries": [
                {
                  "t": 1,
                  "level": "info",
                  "event": "test_event",
                  "screen": "feed",
                  "props": { "path": "/feed" }
                }
              ]
            }
            """;
        using HttpResponseMessage response = await PostJsonAsync(client, "/logs", requestJson);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        long logCount = await Database
            .GetCollection<BsonDocument>("logs")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await Assert.That(logCount).IsEqualTo(0);
    }

    [Test]
    public async Task DeleteAccount_RemovesPlayerDataFromMongoAsync()
    {
        const long telegramUserId = 1004;
        using HttpClient client = CreateAuthenticatedClient(telegramUserId);
        using HttpResponseMessage onboarding = await OnboardAsync(client);
        onboarding.EnsureSuccessStatusCode();

        IMongoCollection<BsonDocument> players = Database.GetCollection<BsonDocument>("players");
        BsonDocument player = await players
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        string playerId = player["_id"].AsString;

        using HttpResponseMessage post = await PostJsonAsync(client, "/posts", """{"text":"hello"}""");
        await post.Content.ReadAsStringAsync();
        post.EnsureSuccessStatusCode();
        const string analyticsJson = """
            {
              "session": "test-session",
              "platform": "test",
              "experiments": {},
              "events": [{ "name": "screen_view", "t": 1, "screen": "feed" }]
            }
            """;
        using HttpResponseMessage analytics = await PostJsonAsync(client, "/events", analyticsJson);
        analytics.EnsureSuccessStatusCode();
        await Database.GetCollection<BsonDocument>("invites").InsertOneAsync(
            new BsonDocument("PlayerId", playerId));

        using HttpResponseMessage response = await client.DeleteAsync("/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await AssertCollectionHasNoPlayerAsync("players", new BsonDocument("TelegramUserId", telegramUserId));
        foreach (string collection in new[] { "posts", "threads", "stars_memory", "invites", "challenges", "events", "analytics", "worlds" })
        {
            await AssertCollectionHasNoPlayerAsync(collection, new BsonDocument("PlayerId", playerId));
        }
    }

    private HttpClient CreateAuthenticatedClient(long telegramUserId)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserHeader,
            telegramUserId.ToString(CultureInfo.InvariantCulture));
        return client;
    }

    private async Task AssertCollectionHasNoPlayerAsync(string collectionName, BsonDocument filter)
    {
        long count = await Database
            .GetCollection<BsonDocument>(collectionName)
            .CountDocumentsAsync(filter);
        await Assert.That(count).IsEqualTo(0);
    }

    private async Task<HttpResponseMessage> OnboardAsync(HttpClient client)
    {
        const string requestJson = """
            {
              "birthGroup": "14-17",
              "world": "stage",
              "role": "creator",
              "avatar": "pick0",
              "name": "Player",
              "handle": "player",
              "address": "nick",
              "star": "mira"
            }
            """;
        return await PostJsonAsync(client, "/onboarding", requestJson);
    }

    private async Task<HttpResponseMessage> PostJsonAsync(
        HttpClient client,
        string path,
        string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(path, content);
    }
}
