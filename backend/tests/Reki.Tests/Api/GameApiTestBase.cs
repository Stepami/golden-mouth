using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Worlds;
using Reki.Infrastructure.Jobs;
using Reki.Infrastructure.Serialization;

namespace Reki.Tests.Api;

public abstract class GameApiTestBase(ApiFactory factory)
{
    protected ApiFactory Factory { get; } = factory;

    protected IMongoDatabase Database => Factory.Session.Database;

    [Before(Test)]
    public void ConfigureChatClient()
    {
        Factory.ResetContentModerator();
        Factory.Llm.Respond(CreateChatResponse);
    }

    [After(Test)]
    public Task ClearGameDataAsync(CancellationToken cancellationToken) =>
        Factory.Session.ClearGameDataAsync(cancellationToken);

    [After(Test)]
    public void ResetExternalState() => Factory.Session.ResetExternalState();

    protected static JsonDocument ParsePlayerLayer(ChatMessage layer) =>
        JsonDocument.Parse(layer.Text[(layer.Text.IndexOf("DATA_JSON:", StringComparison.Ordinal) + "DATA_JSON:".Length)..].Trim());

    protected static ChatResponse CreateChatResponse(IEnumerable<ChatMessage> messages)
    {
        var threadReply = messages.Any(message => message.Text.Contains("Вызови thread_reply", StringComparison.Ordinal));
        if (threadReply)
        {
            var policy = messages.First(message => message.Role == ChatRole.System).Text;
            const string marker = "Ответь выбранной звездой ";
            var selected = policy[(policy.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
            return CreateReplyTurnResponse(selected[..selected.IndexOf('.')]);
        }

        var directMessage = messages.Any(message => message.Text.Contains(
            "tool dm",
            StringComparison.Ordinal));
        var response = directMessage
            ? CreateDirectMessageResponse()
            : CreateCommentResponse(includeUnknownArgument: false);
        var platform = messages.First(message => message.Role == ChatRole.System).Text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var start = platform.IndexOf("[", StringComparison.Ordinal);
        var end = platform.IndexOf("\n\nОграничения текущего хода:", StringComparison.Ordinal);
        using var catalog = JsonDocument.Parse(platform[start..end]);
        var stars = catalog.RootElement.EnumerateArray().ToArray();
        var persona = stars.FirstOrDefault(star => star.GetProperty("id").GetString() == "sonya");
        if (persona.ValueKind == JsonValueKind.Undefined)
        {
            persona = stars[0];
        }

        var arguments = response.Messages.SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>().Single().Arguments
            ?? throw new InvalidOperationException("The test response has no arguments.");
        arguments["star_id"] = persona.GetProperty("id").GetString();
        if (!directMessage)
        {
            arguments["author"] = persona.GetProperty("name").GetString();
        }

        return response;
    }

    protected static ChatResponse CreateCommentResponse(bool includeUnknownArgument)
    {
        using var reasons = JsonDocument.Parse(
            """[{"icon":"🔥","label":"в голосе канала"}]""");
        var arguments = new Dictionary<string, object?>
        {
            ["author"] = "Соня",
            ["star_id"] = "sonya",
            ["text"] = "Есть голос. Продолжай в том же духе.",
            ["hook"] = "Вернёмся к этой теме через пару постов",
            ["tone"] = "support",
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

    protected static ChatResponse CreateReplyTurnResponse(string starId, Action<IDictionary<string, object?>>? customize = null)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["star_id"] = starId,
            ["text"] = "Спасибо за ответ. Продолжим обсуждение.",
            ["tone"] = "support",
            ["emoji"] = string.Empty,
            ["additional_star_id"] = string.Empty,
            ["additional_text"] = string.Empty,
            ["additional_tone"] = string.Empty,
        };
        customize?.Invoke(arguments);
        return new ChatResponse(new ChatMessage(
            ChatRole.Assistant,
            [new FunctionCallContent("test-reply-call", "thread_reply", arguments)]));
    }

    protected static ChatResponse CreateDirectMessageResponse()
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

    protected static async Task<string> GetWorldIdAsync(TestApiClient api) =>
        (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().Id;

    protected static IReadOnlyList<ReplyLiveEvent> ParseReplyEvents(string body) =>
        body.Split('\n').Where(line => line.StartsWith("data: ", StringComparison.Ordinal))
            .Select(line => JsonSerializer.Deserialize(line[6..], ApiJsonSerializerContext.Default.ReplyLiveEvent)
                ?? throw new InvalidOperationException("The reply event was empty.")).ToArray();

    protected async Task<PlayerResponse> CreatePlayerWithWorldAsync(
        TestApiClient api,
        WorldId world = WorldId.Stage,
        string referralCode = "")
    {
        var player = await api.Player.CompleteOnboardingAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Teen,
                Address = Address.Nick,
                ReferralCode = referralCode,
            },
            CancellationToken.None);
        if (world == WorldId.Own)
        {
            // Compatibility fixtures represent an already unlocked, historical owner.
            await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
                new BsonDocument("_id", player.Id),
                new BsonDocument("$set", new BsonDocument("HitOwnWorldMilestone", true)));
        }

        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api, world), CancellationToken.None);
        return player;
    }

    protected async Task<CreateWorldRequest> CreateWorldDefinitionAsync(TestApiClient api, WorldId world = WorldId.Stage)
    {
        // Keep historical-world tests independent of the current public catalog.
        var name = $"legacy_fixture_{Guid.NewGuid():N}";
        var database = Factory.Session.MongoClient.GetDatabase(name);
        Domain.Worlds.World legacy;
        try
        {
            using var session = await Factory.Session.MongoClient.StartSessionAsync();
            await new Infrastructure.Persistence.Migrations.SeedWorldTemplates().UpAsync(database, session, CancellationToken.None);
            legacy = await database.GetCollection<Domain.Worlds.World>("world_templates")
                .Find(item => item.BalanceKey == world.ToString().ToLowerInvariant()).SingleAsync();
        }
        finally
        {
            await Factory.Session.MongoClient.DropDatabaseAsync(name);
        }

        var template = new Reki.Api.Mapping.ApiMapper().MapWorld(legacy);
        return DefinitionOf(template);
    }

    protected CreateWorldRequest DefinitionOf(WorldResponse template) => new()
        {
            BalanceKey = template.BalanceKey,
            Title = template.Title,
            Description = template.Description,
            AvatarStyle = template.AvatarStyle,
            Emoji = template.Emoji,
            Summary = template.Summary,
            Difficulty = template.Difficulty,
            UnlockSkill = template.UnlockSkill,
            UnlockLevel = template.UnlockLevel,
            Where = template.Where,
            FirstContext = template.FirstContext,
            FirstPrompt = template.FirstPrompt,
            Roles = template.Roles,
            Voices = template.Voices,
            PostIdeas = template.PostIdeas,
            DemoPosts = template.DemoPosts,
            Rules = template.Rules,
            Stars = template.Stars,
            Wants = template.Wants,
            Achievements = template.Achievements,
            Name = "Player",
            Avatar = "player",
            Role = "creator",
            Voice = template.Voice,
            Drama = template.Drama,
        };

    protected async Task ResetGameDayAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ResetGameDayJob>().ExecuteAsync(new(), CancellationToken.None);
    }

    protected async Task<string> WorldPathAsync(HttpClient http, string path)
    {
        var user = http.DefaultRequestHeaders.TryGetValues(TestAuthenticationHandler.UserIdHeaderName, out var values)
            ? long.Parse(values.Single(), System.Globalization.CultureInfo.InvariantCulture) : 0;
        var player = await Database.GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", user)).SingleOrDefaultAsync();
        var world = player is null ? null : await Database.GetCollection<BsonDocument>("worlds")
            .Find(new BsonDocument("Player.PlayerId", player["_id"])).SingleOrDefaultAsync();
        return $"{path}?worldId={world?["_id"].AsString ?? "missing-world"}";
    }

    protected async Task<(string PostId, string CommentId)> PublishPostForThreadAsync(long telegramUserId)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(api);
        await PublishAsync(telegramUserId, "пост для треда");
        var player = await GetStoredPlayerAsync(telegramUserId);
        var post = await Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("AuthorId", $"Player_{player["_id"]}"))
            .SingleAsync();
        return (post["_id"].AsString, post["Comments"].AsBsonArray[0]["_id"].AsString);
    }

    protected Task<BsonDocument> GetStoredPlayerAsync(long telegramUserId) =>
        Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

    protected Task<BsonDocument> GetStoredPostAsync(string postId) =>
        Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("_id", postId))
            .SingleAsync();

    protected async Task<ReplyLiveEvent> ReplyAsync(
        long telegramUserId,
        string postId,
        string commentId,
        string replyId,
        string text)
    {
        using var http = CreateAuthenticatedHttpClient(telegramUserId);
        var request = new ReplyRequest
        {
            CommentId = commentId,
            RequestId = replyId,
            Text = text,
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.ReplyRequest),
            Encoding.UTF8,
            "application/json");
        using var response = await http.PostAsync(
            await WorldPathAsync(http, $"/posts/{postId}/replies"),
            body,
            CancellationToken.None);
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync(CancellationToken.None);
        return ParseReplyEvents(json).Last(item => item.Status == ReplyStatus.Completed);
    }

    protected IReadOnlyList<ChatMessage> GetCapturedChatMessages()
    {
        return Factory.Llm.GetLastMessages();
    }

    protected Task<(System.Net.HttpStatusCode StatusCode, string Body, string PublicationId)> GenerateAsync(long telegramUserId, string text) =>
        GenerateWithIdAsync(telegramUserId, text, Guid.NewGuid().ToString());

    protected async Task<(System.Net.HttpStatusCode StatusCode, string Body, string PublicationId)> GenerateWithIdAsync(
        long telegramUserId,
        string text,
        string publicationId)
    {
        using var http = CreateAuthenticatedHttpClient(telegramUserId);
        var request = new StartPublicationRequest
        {
            PublicationId = publicationId,
            Text = text,
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.StartPublicationRequest),
            Encoding.UTF8,
            "application/json");
        using var response = await http.PostAsync(
            await WorldPathAsync(http, "/posts/publications"),
            body,
            CancellationToken.None);
        return (
            response.StatusCode,
            await response.Content.ReadAsStringAsync(CancellationToken.None),
            publicationId);
    }

    protected Task PublishAsync(long telegramUserId, string text) =>
        PublishWithIdAsync(telegramUserId, text, Guid.NewGuid().ToString());

    protected async Task PublishWithIdAsync(long telegramUserId, string text, string requestedPublicationId)
    {
        (var statusCode, var generation, var publicationId) =
            await GenerateWithIdAsync(telegramUserId, text, requestedPublicationId);
        await Assert.That(statusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(generation).Contains("\"type\":\"ready_for_moderation\"");
        using var http = CreateAuthenticatedHttpClient(telegramUserId);
        using var moderation = await http.PostAsync(
            await WorldPathAsync(http, $"/posts/publications/{publicationId}/moderation"),
            null,
            CancellationToken.None);
        await Assert.That(moderation.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(await moderation.Content.ReadAsStringAsync(CancellationToken.None))
            .Contains("\"type\":\"result\"");
    }

    protected HttpClient CreateAuthenticatedHttpClient(long telegramUserId)
    {
        var http = Factory.CreateClient();
        http.DefaultRequestHeaders.Add(
            TestAuthenticationHandler.UserIdHeaderName,
            telegramUserId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return http;
    }

    protected async Task AssertCollectionHasNoPlayerAsync(string collectionName, BsonDocument filter)
    {
        var count = await Database
            .GetCollection<BsonDocument>(collectionName)
            .CountDocumentsAsync(filter);
        await Assert.That(count).IsEqualTo(0);
    }

    protected async Task<BsonDocument> GetStoredWorldAsync(long telegramUserId)
    {
        var player = await GetStoredPlayerAsync(telegramUserId);
        return await Database.GetCollection<BsonDocument>("worlds")
            .Find(new BsonDocument("Player.PlayerId", player["_id"]))
            .SingleAsync(CancellationToken.None);
    }
}