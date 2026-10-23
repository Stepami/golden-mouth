using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Player;
using Reki.Api.Requests.Posts;
using Reki.Api.Requests.Worlds;
using Reki.Api.Serialization;
using Reki.Application.Player;
using Reki.Application.Player.Responses;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Application.Posts.Responses.ReplyLiveEvent;
using Reki.Application.Worlds;
using Reki.Application.Worlds.Responses;
using Reki.Infrastructure.Jobs;

namespace Reki.Tests.Api;

public abstract class GameApiTestBase(ApiFactory factory)
{
    protected ApiFactory Factory { get; } = factory;

    protected IMongoDatabase Database => Factory.Session.Database;

    [Before(Test)]
    public void ConfigureChatClient()
    {
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

        var response = CreateCommentResponse(includeUnknownArgument: false);
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
        arguments["author"] = persona.GetProperty("name").GetString();

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

    protected static async Task<string> GetWorldIdAsync(TestApiClient api) =>
        (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().Id.ToString();

    protected static IReadOnlyList<ReplyLiveEvent> ParseReplyEvents(string body) =>
        body.Split('\n').Where(line => line.StartsWith("data: ", StringComparison.Ordinal))
            .Select(line => JsonSerializer.Deserialize(line[6..], ApiJsonSerializerContext.Default.ReplyLiveEvent)
                ?? throw new InvalidOperationException("The reply event was empty.")).ToArray();

    protected async Task<PlayerResponse> CreatePlayerWithWorldAsync(
        TestApiClient api,
        WorldTemplate world = WorldTemplate.Stage,
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
        if (world == WorldTemplate.Own)
        {
            // Compatibility fixtures represent an already unlocked, historical owner.
            await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
                new BsonDocument("_id", new BsonBinaryData(Guid.Parse(player.Id), GuidRepresentation.Standard)),
                new BsonDocument("$set", new BsonDocument("HitOwnWorldMilestone", true)));
        }

        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api, world), CancellationToken.None);
        return player;
    }

    protected Task<CreateWorldRequest> CreateWorldDefinitionAsync(TestApiClient api, WorldTemplate world = WorldTemplate.Stage)
    {
        // Keep historical-world tests independent of the current public catalog.
        var legacy = Factory.Session.LegacyWorldTemplates[world.ToString().ToLowerInvariant()];
        var template = legacy.ToResponse();
        return Task.FromResult(DefinitionOf(template) with
        {
            TemplateId = world switch
            {
                WorldTemplate.Own => null,
                WorldTemplate.It => Guid.Parse("5720946a-ea7c-4a10-9c50-51ce3deefc53"),
                WorldTemplate.Stage => Guid.Parse("b3a1a522-0263-4796-a197-57a8731fc291"),
                _ => Guid.Parse("8c02a7fb-3b55-4eb0-b957-1b4869ed9834"),
            },
        });
    }

    protected CreateWorldRequest DefinitionOf(WorldResponse template) => new()
        {
            TemplateId = template.Id,
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
            Rules = template.Rules.Select(rule => new WorldRuleRequest(rule.Text, rule.Locked)).ToArray(),
            Stars = template.Stars.Select(star => new WorldStarRequest(star.Id, star.Name, star.Role, star.Trait, star.About, star.Voice, star.AvatarSeed, star.Relation)).ToArray(),
            Wants = template.Wants.Select(want => new WantRequest(want.Id, want.Text, want.Reward, want.Done)).ToArray(),
            Achievements = template.Achievements.Select(card => new CollectionCardRequest(card.Id, card.Emoji, card.Title, card.Open)).ToArray(),
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

    protected async Task<(string PostId, string CommentId)> PublishPostForThreadAsync(long telegramUserId)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        await CreatePlayerWithWorldAsync(api);
        await PublishAsync(telegramUserId, "пост для треда");
        var player = await GetStoredPlayerAsync(telegramUserId);
        var post = await Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("AuthorId", $"Player_{player["_id"].AsBsonBinaryData.ToGuid():N}"))
            .SingleAsync();
        return (post["_id"].AsBsonBinaryData.ToGuid().ToString("N"), post["Comments"].AsBsonArray[0]["_id"].AsBsonBinaryData.ToGuid().ToString("N"));
    }

    protected Task<BsonDocument> GetStoredPlayerAsync(long telegramUserId) =>
        Database
            .GetCollection<BsonDocument>("players")
            .Find(new BsonDocument("TelegramUserId", telegramUserId))
            .SingleAsync();

    protected Task<BsonDocument> GetStoredPostAsync(string postId) =>
        Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("_id", new BsonBinaryData(Guid.Parse(postId), GuidRepresentation.Standard)))
            .SingleAsync();

    protected async Task<ReplyLiveEvent> ReplyAsync(
        long telegramUserId,
        string postId,
        string commentId,
        string replyId,
        string text)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        using var response = await api.Posts.ReplyToPostAsync(
            postId,
            await GetWorldIdAsync(api),
            new ReplyRequest { CommentId = commentId, RequestId = replyId, Text = text },
            CancellationToken.None);
        using var reader = new StreamReader(response.Stream);
        var body = await reader.ReadToEndAsync(CancellationToken.None);
        return ParseReplyEvents(body).Last(item => item.Status == ReplyStatus.Completed);
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
        using var api = Factory.CreateApiClient(telegramUserId);
        try
        {
            using var response = await api.Posts.GeneratePublicationAsync(
                await GetWorldIdAsync(api),
                new StartPublicationRequest { PublicationId = publicationId, Text = text },
                CancellationToken.None);
            using var reader = new StreamReader(response.Stream);
            return ((System.Net.HttpStatusCode)response.StatusCode, await reader.ReadToEndAsync(CancellationToken.None), publicationId);
        }
        catch (PostsApiException exception)
        {
            return ((System.Net.HttpStatusCode)exception.StatusCode, exception.Response, publicationId);
        }
    }

    protected Task PublishAsync(long telegramUserId, string text) =>
        PublishWithIdAsync(telegramUserId, text, Guid.NewGuid().ToString());

    protected async Task PublishWithIdAsync(long telegramUserId, string text, string requestedPublicationId)
    {
        (var statusCode, var generation, var publicationId) =
            await GenerateWithIdAsync(telegramUserId, text, requestedPublicationId);
        await Assert.That(statusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(generation).Contains("\"type\":\"ready_for_moderation\"");
        using var api = Factory.CreateApiClient(telegramUserId);
        var events = await api.Posts.ModeratePublicationAsync(
            publicationId, await GetWorldIdAsync(api), CancellationToken.None);
        await Assert.That(events.OfType<ResultLiveEvent>().Any()).IsTrue();
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
