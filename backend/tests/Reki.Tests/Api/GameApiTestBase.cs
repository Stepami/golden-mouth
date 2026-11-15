using System.Text.Json;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Player;
using Reki.Api.Requests.Posts;
using Reki.Api.Requests.Worlds;
using Reki.Api.Serialization;
using Reki.Application.Player;
using Reki.Application.Player.Responses;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.Worlds;
using Reki.Application.Worlds.Responses;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

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
        var threadReply = messages.Any(message => message.Text.Contains("Ответь обычным текстом", StringComparison.Ordinal));
        if (threadReply)
        {
            return CreateReplyTextResponse();
        }

        var response = CreateCommentResponse(includeUnknownArgument: false);
        using var profile = ParsePlayerLayer(messages.Single(message =>
            message.Role == ChatRole.System && message.Text.Contains("DATA_JSON:", StringComparison.Ordinal)));
        var stars = profile.RootElement.GetProperty("world").GetProperty("stars").EnumerateArray().ToArray();
        var persona = stars.FirstOrDefault(star => star.GetProperty("id").GetString() == "sonya");
        if (persona.ValueKind == JsonValueKind.Undefined)
        {
            persona = stars[0];
        }

        var arguments = response.Messages.SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>().Single().Arguments
            ?? throw new InvalidOperationException("The test response has no arguments.");
        arguments["star_id"] = persona.GetProperty("id").GetString();

        return response;
    }

    protected static ChatResponse CreateCommentResponse(bool includeUnknownArgument)
    {
        using var reasons = JsonDocument.Parse(
            """[{"icon":"🔥","label":"в голосе канала"}]""");
        var arguments = new Dictionary<string, object?>
        {
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

    protected static ChatResponse CreateReplyTextResponse(string text = "Спасибо за ответ. Продолжим обсуждение.") =>
        new(new ChatMessage(ChatRole.Assistant, text));

    protected static async Task<string> GetWorldIdAsync(TestApiClient api) =>
        (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single().Id.ToString();

    protected static async Task<string[]> GetWorldAuthorsAsync(TestApiClient api)
    {
        var player = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var world = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single();
        return [player.Id.ToAuthorId().Value, .. world.Stars.Select(star => star.Id.ToAuthorId().Value)];
    }

    protected static async Task<string[]> GetPlayerAuthorAsync(TestApiClient api) =>
        [(await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).Id.ToAuthorId().Value];

    protected static IReadOnlyList<ReplyGenerationLiveEvent> ParseReplyGenerationEvents(string body) =>
        body.Split('\n').Where(line => line.StartsWith("data: ", StringComparison.Ordinal))
            .Select(line => JsonSerializer.Deserialize(line[6..], ApiJsonSerializerContext.Default.ReplyGenerationLiveEvent)
                ?? throw new InvalidOperationException("The generation event was empty.")).ToArray();

    protected async Task<PlayerResponse> CreatePlayerWithWorldAsync(
        TestApiClient api,
        WorldTemplate world = WorldTemplate.Stage,
        string referralCode = "")
    {
        var player = await api.RegisterAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Adult,
                BirthYear = 1990,
                Address = Address.Nick,
                ReferralCode = referralCode,
            },
            CancellationToken.None);
        if (world == WorldTemplate.Own)
        {
            // Compatibility fixtures represent an already unlocked, historical owner.
            await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
                new BsonDocument("_id", new BsonBinaryData(player.Id.Value, GuidRepresentation.Standard)),
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
                WorldTemplate.It => WorldId.From("5720946a-ea7c-4a10-9c50-51ce3deefc53"),
                WorldTemplate.Stage => WorldId.From("b3a1a522-0263-4796-a197-57a8731fc291"),
                _ => WorldId.From("8c02a7fb-3b55-4eb0-b957-1b4869ed9834"),
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
            Stars = template.Stars.Select(star => new WorldStarRequest(star.Id, star.Name, star.Role, star.Trait, star.About, star.Voice, star.AvatarSeed)).ToArray(),
            Achievements = template.Achievements.Select(card => new CollectionCardRequest(card.Id, card.Emoji, card.Title, card.Open)).ToArray(),
            Name = "Player",
            Avatar = "player",
            Role = "creator",
            Voice = template.Voice,
            Drama = template.Drama,
        };

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
            .Find(new BsonDocument("ExternalUserIds", $"telegram:{telegramUserId}"))
            .SingleAsync();

    protected Task<BsonDocument> GetStoredPostAsync(string postId) =>
        Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("_id", new BsonBinaryData(Guid.Parse(postId), GuidRepresentation.Standard)))
            .SingleAsync();

    protected async Task<ReplyResponse> ReplyAsync(
        long telegramUserId,
        string postId,
        string commentId,
        string replyId,
        string text)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        var player = await GetStoredPlayerAsync(telegramUserId);
        var worldId = await GetWorldIdAsync(api);
        var response = await api.Posts.ReplyToPostAsync(
            postId,
            worldId,
            new ReplyRequest
            {
                AuthorId = PlayerId.From(player["_id"].AsBsonBinaryData.ToGuid()).ToAuthorId(),
                CommentId = Domain.Posts.CommentId.From(commentId),
                RequestId = replyId,
                Text = text,
            },
            CancellationToken.None);
        var target = response.Post.Comments.Single(item => item.Id == Domain.Posts.CommentId.From(commentId));
        if (response.Post.Comments.Any(item => item.ParentCommentId == response.CommentId && item.AuthorId == target.AuthorId))
        {
            return response;
        }

        using var generation = await api.Posts.GenerateReplyAsync(
            postId,
            worldId,
            new GenerateReplyRequest
            {
                AuthorId = target.AuthorId,
                SourceCommentId = response.CommentId,
                Additional = false,
            },
            CancellationToken.None);
        using var reader = new StreamReader(generation.Stream);
        var events = (await reader.ReadToEndAsync(CancellationToken.None)).Split('\n')
            .Where(line => line.StartsWith("data: ", StringComparison.Ordinal))
            .Select(line => JsonSerializer.Deserialize(line[6..], ApiJsonSerializerContext.Default.ReplyGenerationLiveEvent))
            .ToArray();
        var completed = events.Last(item => item?.Status == ReplyGenerationStatus.Completed)
            ?? throw new InvalidOperationException("No generated star reply.");
        var star = await api.Posts.ReplyToPostAsync(
            postId,
            worldId,
            new ReplyRequest
            {
                AuthorId = completed.AuthorId,
                CommentId = response.CommentId,
                RequestId = replyId + ":star",
                Text = completed.Text ?? throw new InvalidOperationException("No generated text."),
            },
            CancellationToken.None);
        return star;
    }

    protected IReadOnlyList<ChatMessage> GetCapturedChatMessages()
    {
        return Factory.Llm.GetLastMessages();
    }

    protected async Task<(System.Net.HttpStatusCode StatusCode, string Body, string PublicationId)> GenerateAsync(long telegramUserId, string text)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        try
        {
            await api.Posts.CreatePostAsync(await GetWorldIdAsync(api), new CreatePostRequest { Text = text }, CancellationToken.None);
            return (System.Net.HttpStatusCode.OK, string.Empty, string.Empty);
        }
        catch (PostsApiException exception)
        {
            return ((System.Net.HttpStatusCode)exception.StatusCode, exception.Response, string.Empty);
        }
    }

    protected async Task PublishAsync(long telegramUserId, string text)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        var worldId = await GetWorldIdAsync(api);
        var created = await api.Posts.CreatePostAsync(worldId, new CreatePostRequest { Text = text }, CancellationToken.None);
        using var generation = await api.Posts.GenerateReplyAsync(
            created.Post.PostId.ToString(),
            worldId,
            new GenerateReplyRequest { AuthorId = created.PredictedAuthorId, SourceCommentId = null, Additional = false },
            CancellationToken.None);
        using var reader = new StreamReader(generation.Stream);
        var completed = ParseReplyGenerationEvents(await reader.ReadToEndAsync(CancellationToken.None))
            .Last(item => item.Status == ReplyGenerationStatus.Completed);
        await api.Posts.ReplyToPostAsync(
            created.Post.PostId.ToString(),
            worldId,
            new ReplyRequest { AuthorId = completed.AuthorId, Text = completed.Text ?? string.Empty, RequestId = Guid.NewGuid().ToString("N") },
            CancellationToken.None);
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
