using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Reki.Api.Serialization;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using Reki.Contracts.Worlds;
using Reki.Tests.Generated;

namespace Reki.Tests.Api;

[NotInParallel]
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class GameApiTests(ApiFactory factory)
{
    private ChatOptions? _lastChatOptions;
    private IReadOnlyList<ChatMessage>? _lastChatMessages;

    private IMongoDatabase Database => new MongoClient(factory.MongoConnectionString)
        .GetDatabase(factory.DatabaseName);

    [Before(Test)]
    public void ConfigureChatClient()
    {
        _lastChatOptions = null;
        _lastChatMessages = null;
        factory.ChatClient.ClearReceivedCalls();
        factory.ChatClient
            .GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                ChatMessage[] messages = callInfo
                    .ArgAt<IEnumerable<ChatMessage>>(0)
                    .ToArray();
                _lastChatMessages = messages;
                _lastChatOptions = callInfo.ArgAt<ChatOptions?>(1)?.Clone();
                return Task.FromResult(CreateChatResponse(messages));
            });
        factory.ChatClient
            .GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                ChatMessage[] messages = callInfo
                    .ArgAt<IEnumerable<ChatMessage>>(0)
                    .ToArray();
                _lastChatMessages = messages;
                _lastChatOptions = callInfo.ArgAt<ChatOptions?>(1)?.Clone();
                return StreamResponseAsync(CreateChatResponse(messages));
            });
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
        await Assert.That(player["_id"].BsonType).IsEqualTo(BsonType.String);
        await Assert.That(player["TelegramUserId"].BsonType).IsEqualTo(BsonType.Int64);
        await Assert.That(player["World"].AsString).IsEqualTo("stage");
        await Assert.That(player["Skills"].AsBsonDocument.Names)
            .IsEquivalentTo(["brave", "humor", "topic"]);
        await Assert.That(player["BirthGroup"].AsString).IsEqualTo("14-17");
        await Assert.That(player.Names.Any(name => name.Contains("year", StringComparison.OrdinalIgnoreCase)))
            .IsFalse();
    }

    [Test]
    public async Task Feed_UsesPlayerProfileForPostAuthorAsync()
    {
        // Arrange
        const long telegramUserId = 1023;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        await PublishAsync(telegramUserId, "пост для ленты");

        // Act
        IReadOnlyList<FeedItemResponse> feed = await api.Posts.GetFeedAsync(CancellationToken.None);

        // Assert
        await Assert.That(feed.Count).IsEqualTo(1);
        await Assert.That(feed[0].Text).IsEqualTo("пост для ленты");
        await Assert.That(feed[0].Author).IsEqualTo("Player");
        await Assert.That(feed[0].Avatar).IsEqualTo("pick0");
        await Assert.That(feed[0].Role).IsEqualTo("creator");
    }

    [Test]
    public async Task Publication_StreamingModeration_ExposesCommittedRecoveryStatusAsync()
    {
        const long telegramUserId = 1020;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        factory.ChatClient.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(StreamCommentAsync());
        using HttpClient http = factory.CreateClient();
        http.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            telegramUserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        string publicationId = Guid.NewGuid().ToString();
        using var body = new StringContent(
            $$"""{"publicationId":"{{publicationId}}","text":"безопасный пост"}""",
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage generation = await http.PostAsync(
            "/posts/publications",
            body,
            CancellationToken.None);
        string generationSse = await generation.Content.ReadAsStringAsync(CancellationToken.None);
        await Assert.That(generation.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(generationSse).Contains("\"type\":\"pending\"");
        await Assert.That(generationSse.Split("\"type\":\"pending\"", StringSplitOptions.None).Length)
            .IsGreaterThan(2);
        await Assert.That(generationSse).Contains("\"type\":\"ready_for_moderation\"");
        BsonDocument ready = await Database.GetCollection<BsonDocument>("publications")
            .Find(new BsonDocument("_id", publicationId))
            .SingleAsync();
        await Assert.That(ready["ExpiresAtUtc"].IsBsonNull).IsTrue();

        using var duplicateBody = new StringContent(
            $$"""{"publicationId":"{{publicationId}}","text":"безопасный пост"}""",
            Encoding.UTF8,
            "application/json");
        using HttpResponseMessage duplicateGeneration = await http.PostAsync(
            "/posts/publications",
            duplicateBody,
            CancellationToken.None);
        await Assert.That(duplicateGeneration.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(await duplicateGeneration.Content.ReadAsStringAsync(CancellationToken.None))
            .Contains("\"type\":\"ready_for_moderation\"");
        _ = factory.ChatClient.Received(1).GetStreamingResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());

        using HttpResponseMessage moderation = await http.PostAsync(
            $"/posts/publications/{publicationId}/moderation",
            null,
            CancellationToken.None);
        string moderationSse = await moderation.Content.ReadAsStringAsync(CancellationToken.None);
        await Assert.That(moderation.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(moderation.Content.Headers.ContentType?.MediaType).IsEqualTo("text/event-stream");
        await Assert.That(moderationSse).Contains("\"type\":\"views\"");
        await Assert.That(moderationSse).Contains("\"views\":");
        await Assert.That(moderationSse).Contains("\"type\":\"react\"");
        await Assert.That(moderationSse).Contains("\"emoji\":");
        await Assert.That(moderationSse).Contains("\"type\":\"comment\"");
        await Assert.That(moderationSse).Contains("\"comment\":");
        await Assert.That(moderationSse).Contains("\"type\":\"typing\"");
        await Assert.That(moderationSse).Contains("\"who\":");
        await Assert.That(moderationSse).Contains("\"type\":\"result\"");
        await Assert.That(moderationSse).Contains("\"result\":");

        using HttpResponseMessage status = await http.GetAsync(
            $"/posts/publications/{publicationId}",
            CancellationToken.None);
        string statusJson = await status.Content.ReadAsStringAsync(CancellationToken.None);
        await Assert.That(statusJson).Contains("\"status\":\"completed\"");
        await Assert.That(statusJson).Contains("\"result\":{");
        using HttpResponseMessage duplicateModeration = await http.PostAsync(
            $"/posts/publications/{publicationId}/moderation",
            null,
            CancellationToken.None);
        await Assert.That(duplicateModeration.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        PlayerResponse player = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(player.PostsLeft).IsEqualTo(9);
        BsonDocument storedPlayer = await Database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        long postCount = await Database.GetCollection<BsonDocument>("posts")
            .CountDocumentsAsync(new BsonDocument("PlayerId", storedPlayer["_id"]));
        await Assert.That(postCount).IsEqualTo(1);
    }

    [Test]
    public async Task Publication_UnsafeStream_RecordsFailedStatusWithoutChargingAsync()
    {
        const long telegramUserId = 1021;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        ChatResponse unsafeResponse = CreateCommentResponse(false);
        FunctionCallContent call = unsafeResponse.Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .Single();
        IDictionary<string, object?> arguments = call.Arguments
            ?? throw new InvalidOperationException("Tool call has no arguments.");
        arguments["text"] = "наркотики";
        factory.ChatClient.GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(StreamResponseAsync(unsafeResponse));
        using HttpClient http = factory.CreateClient();
        http.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            telegramUserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        string publicationId = Guid.NewGuid().ToString();
        using var body = new StringContent(
            $$"""{"publicationId":"{{publicationId}}","text":"безопасный пост"}""",
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage generation = await http.PostAsync(
            "/posts/publications",
            body,
            CancellationToken.None);
        string sse = await generation.Content.ReadAsStringAsync(CancellationToken.None);
        await Assert.That(sse).Contains("\"type\":\"failed\"");
        BsonDocument publication = await Database.GetCollection<BsonDocument>("publications")
            .Find(new BsonDocument("_id", publicationId))
            .SingleAsync();
        await Assert.That(publication["Parts"].AsBsonArray.Count).IsEqualTo(0);
        await Assert.That(publication["ExpiresAtUtc"].BsonType).IsEqualTo(BsonType.DateTime);
        PlayerResponse player = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(player.PostsLeft).IsEqualTo(10);
        _ = factory.ChatClient.Received(1).GetStreamingResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task LegacyPostRoute_AfterRemoval_ReturnsNotFoundAsync()
    {
        using HttpClient http = CreateAuthenticatedHttpClient(1022);
        using var body = new StringContent("{\"text\":\"hello\"}", Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await http.PostAsync(
            "/posts",
            body,
            CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task PublishPost_AfterDailyLimit_ReturnsConflictAsync()
    {
        const long telegramUserId = 1002;
        using TestApiClient api = factory.CreateApiClient(1002);
        await OnboardAsync(api);

        for (int postIndex = 0; postIndex < 10; postIndex++)
        {
            await PublishAsync(telegramUserId, $"post {postIndex}");
        }

        (System.Net.HttpStatusCode statusCode, _, _) = await GenerateAsync(
            telegramUserId,
            "over limit");
        await Assert.That(statusCode).IsEqualTo(System.Net.HttpStatusCode.Conflict);
    }

    [Test]
    public async Task PublishPost_WhenLlmReturnsUnknownArgument_DoesNotConsumePostAsync()
    {
        const long telegramUserId = 1007;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        factory.ChatClient
            .GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(StreamResponseAsync(CreateCommentResponse(includeUnknownArgument: true)));

        (_, string generation, string publicationId) = await GenerateAsync(
            telegramUserId,
            "обычный безопасный пост");

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();
        long postCount = await Database
            .GetCollection<BsonDocument>("posts")
            .CountDocumentsAsync(new BsonDocument("PlayerId", player["_id"]));

        await Assert.That(generation).Contains("\"type\":\"failed\"");
        BsonDocument publication = await Database.GetCollection<BsonDocument>("publications")
            .Find(new BsonDocument("_id", publicationId))
            .SingleAsync();
        await Assert.That(publication["ExpiresAtUtc"].BsonType).IsEqualTo(BsonType.DateTime);
        await Assert.That(player["PostsUsed"].AsInt32).IsEqualTo(0);
        await Assert.That(postCount).IsEqualTo(0);
    }

    [Test]
    public async Task PublishPost_UsesExplicitConfiguredGenerationOptionsAsync()
    {
        using TestApiClient api = factory.CreateApiClient(1008);
        await OnboardAsync(api);

        await PublishAsync(1008, "короткий пост про сцену");

        ChatOptions? options = _lastChatOptions;
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
        factory.ChatClient
            .GetStreamingResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(StreamTimeoutAsync());

        (_, string generation, _) = await GenerateAsync(
            telegramUserId,
            "безопасный пост перед таймаутом");

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

        await Assert.That(generation).Contains("\"type\":\"failed\"");
        await Assert.That(player["PostsUsed"].AsInt32).IsEqualTo(0);
    }

    [Test]
    public async Task PublishPost_WhenCrisisSignalDetected_AppliesCrisisSafetyStateAsync()
    {
        const long telegramUserId = 1010;
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);

        (_, string generation, _) = await GenerateAsync(telegramUserId, "я не хочу жить");

        BsonDocument player = await Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

        await Assert.That(generation).Contains("\"type\":\"failed\"");
        await Assert.That(player["PostsUsed"].AsInt32).IsEqualTo(0);
        await Assert.That(player["PausedAtUtc"].AsBsonDocument["DateTime"].ToUniversalTime())
            .IsEqualTo(new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc));
        await Assert.That(player["HateDisabledUntilUtc"].AsBsonDocument["DateTime"].ToUniversalTime())
            .IsEqualTo(new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc));
        _ = factory.ChatClient.DidNotReceive().GetStreamingResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            Arg.Any<CancellationToken>());
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
        await Assert.That(safetyRule.Text).IsEqualTo("Без тем 18+ и травли по внешности");
    }

    [Test]
    public async Task PublishPost_UsesPlayerOwnedWorldSettingsSystemLayerAsync()
    {
        // Arrange
        const long telegramUserId = 1012;
        const string currentPost = "Сегодня разбираю сценическую уверенность";
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api, WorldId.Own);
        await api.Worlds.SaveOwnWorldAsync(
            new UpdateWorldRulesRequest
            {
                Title = "Театральная мастерская",
                Presets = [],
                Description = "Камерный мир молодых режиссёров и актёров.",
                Rules = [new WorldRuleResponse("Звёзды спорят о постановках, но поддерживают дебютантов")],
            },
            CancellationToken.None);
        await api.Player.UpdateSettingsAsync(
            new UpdateSettingsRequest
            {
                Drama = Drama.Hot,
                Voice = "Иронично, тепло и короткими фразами",
            },
            CancellationToken.None);

        // Act
        await PublishAsync(telegramUserId, currentPost);

        // Assert
        IReadOnlyList<ChatMessage> messages = GetCapturedChatMessages();
        ChatMessage playerOwnedLayer = messages.Single(message =>
            message.Role == ChatRole.System &&
            message.Text.Contains("ПОЛЬЗОВАТЕЛЬСКИЙ СИСТЕМНЫЙ СЛОЙ", StringComparison.Ordinal));
        ChatMessage userMessage = messages.Single(message => message.Role == ChatRole.User);
        await Assert.That(messages.Count(message => message.Role == ChatRole.System)).IsEqualTo(2);
        await Assert.That(playerOwnedLayer.Text).Contains("Мир: Театральная мастерская (own)");
        await Assert.That(playerOwnedLayer.Text)
            .Contains("Описание мира: Камерный мир молодых режиссёров и актёров.");
        await Assert.That(playerOwnedLayer.Text).Contains("Градус драмы: остро");
        await Assert.That(playerOwnedLayer.Text)
            .Contains("Голос канала: Иронично, тепло и короткими фразами");
        await Assert.That(playerOwnedLayer.Text)
            .Contains("Звёзды спорят о постановках, но поддерживают дебютантов");
        await Assert.That(userMessage.Text).Contains(currentPost);
        await Assert.That(messages
                .Where(message => message.Role == ChatRole.System)
                .Any(message => message.Text.Contains(currentPost, StringComparison.Ordinal)))
            .IsFalse();
    }

    [Test]
    public async Task SendDirectMessage_UsesStarMemorySystemLayerWithCurrentUserMessageAsync()
    {
        // Arrange
        const long telegramUserId = 1013;
        const string currentMessage = "Напомни, какой формат мне лучше продолжать";
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        await api.Social.SendDirectMessageAsync(
            "sonya",
            new DirectMessageRequest { Text = "Мне нравятся короткие посты" },
            CancellationToken.None);

        // Act
        await api.Social.SendDirectMessageAsync(
            "sonya",
            new DirectMessageRequest { Text = currentMessage },
            CancellationToken.None);

        // Assert
        IReadOnlyList<ChatMessage> messages = GetCapturedChatMessages();
        ChatMessage playerOwnedLayer = messages.Single(message =>
            message.Role == ChatRole.System &&
            message.Text.Contains("ПОЛЬЗОВАТЕЛЬСКИЙ СИСТЕМНЫЙ СЛОЙ", StringComparison.Ordinal));
        ChatMessage userMessage = messages.Single(message => message.Role == ChatRole.User);
        await Assert.That(messages.Count(message => message.Role == ChatRole.System)).IsEqualTo(2);
        await Assert.That(playerOwnedLayer.Text).Contains("Текущая звезда: sonya");
        await Assert.That(playerOwnedLayer.Text)
            .Contains("Память звезды об игроке: любит короткие посты");
        await Assert.That(userMessage.Text).Contains(currentMessage);
        await Assert.That(messages
                .Where(message => message.Role == ChatRole.System)
                .Any(message => message.Text.Contains(currentMessage, StringComparison.Ordinal)))
            .IsFalse();
    }

    [Test]
    public async Task GetReadyWorldRules_ExposesWorldDataPlusExactLockedSafetyRuleAsync()
    {
        // Arrange
        using TestApiClient api = factory.CreateApiClient(1014);
        await OnboardAsync(api);

        // Act
        WorldRulesResponse rules = await api.Worlds.GetWorldRulesAsync(
            WorldId.Stage,
            CancellationToken.None);

        // Assert
        string visibleRules = string.Join("\n", rules.Rules.Select(rule => rule.Text));
        await Assert.That(visibleRules).Contains("Звёзды:");
        await Assert.That(visibleRules).Contains("Фанаты:");
        await Assert.That(visibleRules).Contains("Тон:");
        await Assert.That(visibleRules).Contains("События:");
        WorldRuleResponse safetyRule = rules.Rules.Single(rule =>
            rule.Text == "Без тем 18+ и травли по внешности");
        await Assert.That(safetyRule.Locked).IsTrue();
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
        await PublishAsync(telegramUserId, "настоящий текст поста для карточки");
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

        await PublishAsync(telegramUserId, "hello");
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
                     "publications",
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

    [Test]
    public async Task ThreadReply_FirstThreeRepliesAwardBonusThenLaterRepliesDoNotAsync()
    {
        // Arrange
        const long telegramUserId = 1101;
        (string postId, string commentId) = await PublishPostForThreadAsync(telegramUserId);

        // Act
        ReplyResponse first = await ReplyAsync(telegramUserId, postId, commentId, "thread-key-1", "первый ответ");
        ReplyResponse second = await ReplyAsync(telegramUserId, postId, commentId, "thread-key-2", "второй ответ");
        ReplyResponse third = await ReplyAsync(telegramUserId, postId, commentId, "thread-key-3", "третий ответ");
        ReplyResponse fourth = await ReplyAsync(telegramUserId, postId, commentId, "thread-key-4", "четвёртый ответ");

        // Assert
        await Assert.That(first.SubscriberBonus).IsEqualTo(9);
        await Assert.That(first.AwardedReplies).IsEqualTo(1);
        await Assert.That(first.ReplyLimit).IsEqualTo(3);
        await Assert.That(first.Reaction).IsEqualTo("🔥");
        await Assert.That(second.SubscriberBonus).IsEqualTo(9);
        await Assert.That(second.SubscriberTotal).IsEqualTo(first.SubscriberTotal + second.SubscriberBonus);
        await Assert.That(third.SubscriberBonus).IsEqualTo(9);
        await Assert.That(third.AwardedReplies).IsEqualTo(3);
        await Assert.That(fourth.SubscriberBonus).IsEqualTo(0);
        await Assert.That(fourth.AwardedReplies).IsEqualTo(3);
        await Assert.That(fourth.SubscriberTotal).IsEqualTo(third.SubscriberTotal);

        BsonDocument stored = await GetStoredPostAsync(postId);
        await Assert.That(stored["Comments"].AsBsonArray.Count).IsEqualTo(5);
        await Assert.That(stored["ThreadReplies"].AsBsonArray.Count).IsEqualTo(4);
        BsonDocument player = await GetStoredPlayerAsync(telegramUserId);
        await Assert.That(player["Subscribers"].ToInt64()).IsEqualTo(fourth.SubscriberTotal);
    }

    [Test]
    public async Task ThreadReply_RetryWithSameReplyId_ReturnsSavedOutcomeWithoutNewCommentOrBonusAsync()
    {
        // Arrange
        const long telegramUserId = 1102;
        (string postId, string commentId) = await PublishPostForThreadAsync(telegramUserId);
        const string replyId = "thread-stable-key";

        // Act
        ReplyResponse first = await ReplyAsync(telegramUserId, postId, commentId, replyId, "ответ");
        ReplyResponse retry = await ReplyAsync(telegramUserId, postId, commentId, replyId, "другой текст");

        // Assert
        await Assert.That(retry.CommentId).IsEqualTo(first.CommentId);
        await Assert.That(retry.SubscriberBonus).IsEqualTo(first.SubscriberBonus);
        await Assert.That(retry.SubscriberTotal).IsEqualTo(first.SubscriberTotal);
        await Assert.That(retry.Reaction).IsEqualTo(first.Reaction);

        BsonDocument stored = await GetStoredPostAsync(postId);
        await Assert.That(stored["Comments"].AsBsonArray.Count).IsEqualTo(2);
        await Assert.That(stored["ThreadReplies"].AsBsonArray.Count).IsEqualTo(1);
        BsonDocument player = await GetStoredPlayerAsync(telegramUserId);
        await Assert.That(player["Subscribers"].ToInt64()).IsEqualTo(first.SubscriberTotal);
    }

    [Test]
    public async Task ThreadReply_UnknownTargetComment_ReturnsNotFoundAsync()
    {
        // Arrange
        const long telegramUserId = 1103;
        (string postId, _) = await PublishPostForThreadAsync(telegramUserId);
        using HttpClient http = CreateAuthenticatedHttpClient(telegramUserId);
        var request = new ReplyRequest
        {
            CommentId = Guid.NewGuid().ToString("N"),
            ReplyId = "thread-missing-target",
            Text = "ответ в пустоту",
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.ReplyRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        using HttpResponseMessage response = await http.PostAsync(
            $"/posts/{postId}/replies",
            body,
            CancellationToken.None);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ThreadReply_OnAnotherPlayerPost_ReturnsNotFoundAsync()
    {
        // Arrange
        const long ownerUserId = 1104;
        const long strangerUserId = 1105;
        (string postId, string commentId) = await PublishPostForThreadAsync(ownerUserId);
        using TestApiClient strangerApi = factory.CreateApiClient(strangerUserId);
        await OnboardAsync(strangerApi);
        using HttpClient http = CreateAuthenticatedHttpClient(strangerUserId);
        var request = new ReplyRequest
        {
            CommentId = commentId,
            ReplyId = "thread-foreign-post",
            Text = "чужой пост",
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.ReplyRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        using HttpResponseMessage response = await http.PostAsync(
            $"/posts/{postId}/replies",
            body,
            CancellationToken.None);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.NotFound);
    }

    private static ChatResponse CreateChatResponse(IEnumerable<ChatMessage> messages)
    {
        bool directMessage = messages.Any(message => message.Text.Contains(
            "tool dm",
            StringComparison.Ordinal));
        return directMessage
            ? CreateDirectMessageResponse()
            : CreateCommentResponse(includeUnknownArgument: false);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamCommentAsync()
    {
        await foreach (ChatResponseUpdate update in StreamResponseAsync(CreateCommentResponse(false)))
        {
            yield return update;
        }
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamTimeoutAsync()
    {
        await Task.Yield();
        yield return await Task.FromException<ChatResponseUpdate>(new TimeoutException());
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamResponseAsync(ChatResponse response)
    {
        await Task.Yield();
        foreach (ChatResponseUpdate update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    private static ChatResponse CreateCommentResponse(bool includeUnknownArgument)
    {
        using JsonDocument reasons = JsonDocument.Parse(
            """[{"icon":"🔥","label":"в голосе канала"}]""");
        var arguments = new Dictionary<string, object?>
        {
            ["author"] = "Соня",
            ["star_id"] = "sonya",
            ["text"] = "Есть голос. Продолжай в том же духе.",
            ["hook"] = "Соня вернётся к этой теме через пару постов",
            ["brave"] = 0.7,
            ["humor"] = 0.6,
            ["topic"] = 0.8,
            ["why"] = reasons.RootElement.Clone(),
            ["on_topic"] = true,
        };
        if (includeUnknownArgument)
        {
            arguments["followers"] = 1_000_000;
        }

        var call = new FunctionCallContent("test-call", "comment", arguments);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, [call]));
    }

    private static ChatResponse CreateDirectMessageResponse()
    {
        var arguments = new Dictionary<string, object?>
        {
            ["star_id"] = "sonya",
            ["text"] = "Вижу, о чём ты. Вернёмся к этому после следующего поста.",
            ["memory_fact"] = "любит короткие посты",
        };
        var call = new FunctionCallContent("test-dm-call", "dm", arguments);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, [call]));
    }

    private static Task<PlayerResponse> OnboardAsync(
        TestApiClient api,
        WorldId world = WorldId.Stage)
    {
        return api.Player.CompleteOnboardingAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Teen,
                World = world,
                Role = "creator",
                Avatar = "pick0",
                Name = "Player",
                Handle = "player",
                Address = Address.Nick,
                Star = "mira",
            },
            CancellationToken.None);
    }

    private async Task<(string PostId, string CommentId)> PublishPostForThreadAsync(long telegramUserId)
    {
        using TestApiClient api = factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        await PublishAsync(telegramUserId, "пост для треда");
        BsonDocument player = await GetStoredPlayerAsync(telegramUserId);
        BsonDocument post = await Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("PlayerId", player["_id"]))
            .SingleAsync();
        return (post["_id"].AsString, post["Comments"].AsBsonArray[0]["_id"].AsString);
    }

    private Task<BsonDocument> GetStoredPlayerAsync(long telegramUserId) =>
        Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

    private Task<BsonDocument> GetStoredPostAsync(string postId) =>
        Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("_id", postId))
            .SingleAsync();

    private async Task<ReplyResponse> ReplyAsync(
        long telegramUserId,
        string postId,
        string commentId,
        string replyId,
        string text)
    {
        using HttpClient http = CreateAuthenticatedHttpClient(telegramUserId);
        var request = new ReplyRequest
        {
            CommentId = commentId,
            ReplyId = replyId,
            Text = text,
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.ReplyRequest),
            Encoding.UTF8,
            "application/json");
        using HttpResponseMessage response = await http.PostAsync(
            $"/posts/{postId}/replies",
            body,
            CancellationToken.None);
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        string json = await response.Content.ReadAsStringAsync(CancellationToken.None);
        return JsonSerializer.Deserialize(json, ApiJsonSerializerContext.Default.ReplyResponse)
            ?? throw new InvalidOperationException("The reply response was empty.");
    }

    private async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, CancellationToken.None);
        return buffer.ToArray();
    }

    private IReadOnlyList<ChatMessage> GetCapturedChatMessages()
    {
        return _lastChatMessages ??
            throw new InvalidOperationException("The chat client did not receive messages.");
    }

    private async Task<(System.Net.HttpStatusCode StatusCode, string Body, string PublicationId)> GenerateAsync(
        long telegramUserId,
        string text)
    {
        string publicationId = Guid.NewGuid().ToString();
        using HttpClient http = CreateAuthenticatedHttpClient(telegramUserId);
        var request = new StartPublicationRequest
        {
            PublicationId = publicationId,
            Text = text,
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.StartPublicationRequest),
            Encoding.UTF8,
            "application/json");
        using HttpResponseMessage response = await http.PostAsync(
            "/posts/publications",
            body,
            CancellationToken.None);
        return (
            response.StatusCode,
            await response.Content.ReadAsStringAsync(CancellationToken.None),
            publicationId);
    }

    private async Task PublishAsync(long telegramUserId, string text)
    {
        (System.Net.HttpStatusCode statusCode, string generation, string publicationId) =
            await GenerateAsync(telegramUserId, text);
        await Assert.That(statusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(generation).Contains("\"type\":\"ready_for_moderation\"");
        using HttpClient http = CreateAuthenticatedHttpClient(telegramUserId);
        using HttpResponseMessage moderation = await http.PostAsync(
            $"/posts/publications/{publicationId}/moderation",
            null,
            CancellationToken.None);
        await Assert.That(moderation.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(await moderation.Content.ReadAsStringAsync(CancellationToken.None))
            .Contains("\"type\":\"result\"");
    }

    private HttpClient CreateAuthenticatedHttpClient(long telegramUserId)
    {
        HttpClient http = factory.CreateClient();
        http.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            telegramUserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return http;
    }

    private async Task AssertCollectionHasNoPlayerAsync(string collectionName, BsonDocument filter)
    {
        long count = await Database
            .GetCollection<BsonDocument>(collectionName)
            .CountDocumentsAsync(filter);
        await Assert.That(count).IsEqualTo(0);
    }
}
