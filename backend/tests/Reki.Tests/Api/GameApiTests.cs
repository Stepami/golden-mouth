using System.Buffers.Binary;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;
using Reki.Tests.Generated;

namespace Reki.Tests.Api;

[NotInParallel]
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class GameApiTests(ApiFactory factory)
{
    private IMongoDatabase Database => new MongoClient(factory.MongoConnectionString)
        .GetDatabase(factory.DatabaseName);

    [Before(Test)]
    public void ResetChatClient()
    {
        factory.ChatClient.Reset();
    }

    [Test]
    public async Task Me_WithoutTelegramAuthentication_ReturnsUnauthorizedAsync()
    {
        using TestApiClient api = factory.CreateApiClient();

        PlayerApiException? exception = null;
        try
        {
            await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        }
        catch (PlayerApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the unauthorized response.");
        }

        await Assert.That(exception.StatusCode).IsEqualTo(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task Onboarding_WithEnumContracts_PersistsAgeGroupWithoutBirthYearAsync()
    {
        const long telegramUserId = 1001;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);

        PlayerResponse response = await OnboardAsync(api);

        await Assert.That(response.PostsCap).IsEqualTo(10);
        await Assert.That(response.PostsLeft).IsEqualTo(10);
        await Assert.That(response.World).IsEqualTo(WorldId.Stage);

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
        using TestApiClient api = factory.CreateApiClient(1002);
        await OnboardAsync(api);

        for (int postIndex = 0; postIndex < 10; postIndex++)
        {
            await api.Posts.PublishPostAsync(
                new CreatePostRequest { Text = $"post {postIndex}" },
                CancellationToken.None);
        }

        PostsApiException? exception = null;
        try
        {
            await api.Posts.PublishPostAsync(
                new CreatePostRequest { Text = "over limit" },
                CancellationToken.None);
        }
        catch (PostsApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the conflict response.");
        }

        await Assert.That(exception.StatusCode).IsEqualTo(StatusCodes.Status409Conflict);
    }

    [Test]
    public async Task PublishPost_WhenLlmReturnsUnknownArgument_DoesNotConsumePostAsync()
    {
        const long telegramUserId = 1007;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        factory.ChatClient.ReturnUnknownArgumentOnce();

        PostsApiException? exception = null;
        try
        {
            await api.Posts.PublishPostAsync(
                new CreatePostRequest { Text = "обычный безопасный пост" },
                CancellationToken.None);
        }
        catch (PostsApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the LLM error.");
        }

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        long postCount = await Database
            .GetCollection<BsonDocument>("posts")
            .CountDocumentsAsync(new BsonDocument("PlayerId", player["_id"]));

        await Assert.That(exception.StatusCode).IsEqualTo(StatusCodes.Status503ServiceUnavailable);
        await Assert.That(player["PostsUsed"].AsInt32).IsEqualTo(0);
        await Assert.That(postCount).IsEqualTo(0);
    }

    [Test]
    public async Task PublishPost_UsesExplicitConfiguredGenerationOptionsAsync()
    {
        using TestApiClient api = factory.CreateApiClient(1008);
        await OnboardAsync(api);

        await api.Posts.PublishPostAsync(
            new CreatePostRequest { Text = "короткий пост про сцену" },
            CancellationToken.None);

        ChatOptions? options = factory.ChatClient.LastOptions;
        if (options is null)
        {
            throw new InvalidOperationException("The chat client did not receive options.");
        }

        string[] toolNames = options.Tools?
            .OfType<AIFunctionDeclaration>()
            .Select(tool => tool.Name)
            .Order()
            .ToArray() ?? [];
        await Assert.That(options.ModelId).IsEqualTo("gigachat3.5-432b-a28b");
        await Assert.That(options.Temperature).IsEqualTo(0.4f);
        await Assert.That(options.MaxOutputTokens).IsEqualTo(700);
        await Assert.That(options.Reasoning).IsNull();
        await Assert.That(toolNames).IsEquivalentTo(
            new[] { "comment", "dm", "react", "repost", "start_event" });
    }

    [Test]
    public async Task PublishPost_WhenLlmTimesOut_DoesNotConsumePostAsync()
    {
        const long telegramUserId = 1009;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        factory.ChatClient.ThrowOnce(new TimeoutException());

        PostsApiException? exception = null;
        try
        {
            await api.Posts.PublishPostAsync(
                new CreatePostRequest { Text = "безопасный пост перед таймаутом" },
                CancellationToken.None);
        }
        catch (PostsApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the timeout.");
        }

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

        await Assert.That(exception.StatusCode).IsEqualTo(StatusCodes.Status503ServiceUnavailable);
        await Assert.That(player["PostsUsed"].AsInt32).IsEqualTo(0);
    }

    [Test]
    public async Task PublishPost_WhenCrisisSignalDetected_AppliesCrisisSafetyStateAsync()
    {
        const long telegramUserId = 1010;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);

        PostsApiException? exception = null;
        try
        {
            await api.Posts.PublishPostAsync(
                new CreatePostRequest { Text = "я не хочу жить" },
                CancellationToken.None);
        }
        catch (PostsApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the crisis response.");
        }

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

        await Assert.That(exception.StatusCode).IsEqualTo(StatusCodes.Status422UnprocessableEntity);
        await Assert.That(player["PostsUsed"].AsInt32).IsEqualTo(0);
        await Assert.That(player["PausedAtUtc"].AsBsonDocument["DateTime"].ToUniversalTime())
            .IsEqualTo(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc));
        await Assert.That(player["HateDisabledUntilUtc"].AsBsonDocument["DateTime"].ToUniversalTime())
            .IsEqualTo(new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc));
        await Assert.That(factory.ChatClient.Calls).IsEqualTo(0);
    }

    [Test]
    public async Task SaveOwnWorld_WithoutLockedSafetyRule_RestoresRuleAsync()
    {
        using TestApiClient api = factory.CreateApiClient(1011);
        await OnboardAsync(api);
        var request = new UpdateWorldRulesRequest
        {
            Title = "Мой мир",
            Presets = [],
            Description = "Безопасное описание мира",
            Rules = [new WorldRuleResponse("Звёзды любят короткие посты")],
        };

        await api.Worlds.SaveOwnWorldAsync(request, CancellationToken.None);
        WorldRulesResponse rules = await api.Worlds.GetWorldRulesAsync(
            WorldId.Own,
            CancellationToken.None);

        WorldRuleResponse safetyRule = rules.Rules.Single(rule => rule.Locked);
        await Assert.That(safetyRule.Text).IsEqualTo("Контент подходит для 14+");
    }

    [Test]
    public async Task Settings_WhenPlusIsEnabled_UsesPlusDailyLimitAsync()
    {
        using TestApiClient api = factory.CreateApiClient(1005);
        await OnboardAsync(api);

        await api.Player.UpdateSettingsAsync(
            new UpdateSettingsRequest { Plus = true },
            CancellationToken.None);
        PlayerResponse response = await api.Player.GetCurrentPlayerAsync(
            CancellationToken.None);

        await Assert.That(response.PostsCap).IsEqualTo(20);
        await Assert.That(response.PostsLeft).IsEqualTo(20);
    }

    [Test]
    public async Task ClientLogs_InDevelopment_AreNotWrittenToMongoAsync()
    {
        using TestApiClient api = factory.CreateApiClient(1003);
        await OnboardAsync(api);

        await api.Telemetry.SubmitClientLogsAsync(
            new ClientLogBatchRequest
            {
                Session = "test-session",
                Entries =
                [
                    new ClientLogEntryRequest
                    {
                        Timestamp = 1,
                        Level = ClientLogLevel.Info,
                        Event = "test_event",
                        Screen = "feed",
                    },
                ],
            },
            CancellationToken.None);

        long logCount = await Database
            .GetCollection<BsonDocument>("logs")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        await Assert.That(logCount).IsEqualTo(0);
    }

    [Test]
    public async Task StoryCard_ForStoredPost_RendersPostDataInFullSizePngAsync()
    {
        const long telegramUserId = 1006;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        await api.Posts.PublishPostAsync(
            new CreatePostRequest { Text = "настоящий текст поста для карточки" },
            CancellationToken.None);
        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        BsonDocument post = await Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("PlayerId", player["_id"].AsString))
            .SingleAsync();

        using FileResponse response = await api.StoryCards.GetStoryCardAsync(
            post["_id"].AsString,
            CancellationToken.None);
        byte[] png = await ReadAllBytesAsync(response.Stream);

        await Assert.That(response.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(response.Headers["Content-Type"]).Contains("image/png");
        await Assert.That(png.AsSpan(0, 8).ToArray())
            .IsEquivalentTo(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        await Assert.That(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4))).IsEqualTo(1080);
        await Assert.That(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4))).IsEqualTo(1920);

        await Database
            .GetCollection<BsonDocument>("posts")
            .UpdateOneAsync(
                new BsonDocument("_id", post["_id"]),
                new BsonDocument("$set", new BsonDocument("Text", "другой настоящий текст")));
        using FileResponse updatedResponse = await api.StoryCards.GetStoryCardAsync(
            post["_id"].AsString,
            CancellationToken.None);
        byte[] updatedPng = await ReadAllBytesAsync(updatedResponse.Stream);

        await Assert.That(updatedResponse.StatusCode).IsEqualTo(StatusCodes.Status200OK);
        await Assert.That(png.SequenceEqual(updatedPng)).IsFalse();
    }

    [Test]
    public async Task DeleteAccount_RemovesPlayerDataFromMongoAsync()
    {
        const long telegramUserId = 1004;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);

        IMongoCollection<BsonDocument> players = Database.GetCollection<BsonDocument>("players");
        BsonDocument player = await players
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        string playerId = player["_id"].AsString;

        await api.Posts.PublishPostAsync(
            new CreatePostRequest { Text = "hello" },
            CancellationToken.None);
        await api.Telemetry.SubmitAnalyticsAsync(
            new AnalyticsBatchRequest
            {
                Session = "test-session",
                Platform = "test",
                Experiments = new Dictionary<string, string>(),
                Events =
                [
                    new AnalyticsEventRequest
                    {
                        Name = "screen_view",
                        Timestamp = 1,
                        Screen = "feed",
                    },
                ],
            },
            CancellationToken.None);
        await Database.GetCollection<BsonDocument>("invites").InsertOneAsync(
            new BsonDocument("PlayerId", playerId));

        await api.Player.DeleteCurrentPlayerAsync(CancellationToken.None);

        await AssertCollectionHasNoPlayerAsync(
            "players",
            new BsonDocument("TelegramUserId", telegramUserId));
        foreach (string collection in new[]
                 {
                     "posts",
                     "threads",
                     "stars_memory",
                     "invites",
                     "challenges",
                     "events",
                     "analytics",
                     "worlds",
                 })
        {
            await AssertCollectionHasNoPlayerAsync(collection, new BsonDocument("PlayerId", playerId));
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, CancellationToken.None);
        return buffer.ToArray();
    }

    private static Task<PlayerResponse> OnboardAsync(TestApiClient api)
    {
        return api.Player.CompleteOnboardingAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Teen,
                World = WorldId.Stage,
                Role = "creator",
                Avatar = "pick0",
                Name = "Player",
                Handle = "player",
                Address = Address.Nick,
                Star = "mira",
            },
            CancellationToken.None);
    }

    private async Task AssertCollectionHasNoPlayerAsync(string collectionName, BsonDocument filter)
    {
        long count = await Database
            .GetCollection<BsonDocument>(collectionName)
            .CountDocumentsAsync(filter);
        await Assert.That(count).IsEqualTo(0);
    }
}