using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Serialization;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;

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

    protected static Task<PlayerResponse> OnboardAsync(
        TestApiClient api,
        WorldId world = WorldId.Stage,
        string referralCode = "")
    {
        return api.Player.CompleteOnboardingAsync(
            new OnboardingRequest
            {
                BirthGroup = BirthGroup.Teen,
                World = world,
                Role = "creator",
                Name = "Player",
                Handle = "player",
                Address = Address.Nick,
                Star = world switch
                {
                    WorldId.Academy => "aiko",
                    WorldId.It => "greedy_wolf",
                    WorldId.Stream => "glitch",
                    WorldId.Own => "spark",
                    _ => "mira",
                },
                ReferralCode = referralCode,
            },
            CancellationToken.None);
    }

    protected async Task<(string PostId, string CommentId)> PublishPostForThreadAsync(long telegramUserId)
    {
        using var api = Factory.CreateApiClient(telegramUserId);
        await OnboardAsync(api);
        await PublishAsync(telegramUserId, "пост для треда");
        var player = await GetStoredPlayerAsync(telegramUserId);
        var post = await Database
            .GetCollection<BsonDocument>("posts")
            .Find(new BsonDocument("PlayerId", player["_id"]))
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

    protected async Task<ReplyResponse> ReplyAsync(
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
            ReplyId = replyId,
            Text = text,
        };
        using var body = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerContext.Default.ReplyRequest),
            Encoding.UTF8,
            "application/json");
        using var response = await http.PostAsync(
            $"/posts/{postId}/replies",
            body,
            CancellationToken.None);
        await Assert.That(response.StatusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync(CancellationToken.None);
        return JsonSerializer.Deserialize(json, ApiJsonSerializerContext.Default.ReplyResponse)
            ?? throw new InvalidOperationException("The reply response was empty.");
    }

    protected IReadOnlyList<ChatMessage> GetCapturedChatMessages()
    {
        return Factory.Llm.GetLastMessages();
    }

    protected async Task<(System.Net.HttpStatusCode StatusCode, string Body, string PublicationId)> GenerateAsync(
        long telegramUserId,
        string text)
    {
        var publicationId = Guid.NewGuid().ToString();
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
            "/posts/publications",
            body,
            CancellationToken.None);
        return (
            response.StatusCode,
            await response.Content.ReadAsStringAsync(CancellationToken.None),
            publicationId);
    }

    protected async Task PublishAsync(long telegramUserId, string text)
    {
        (var statusCode, var generation, var publicationId) =
            await GenerateAsync(telegramUserId, text);
        await Assert.That(statusCode).IsEqualTo(System.Net.HttpStatusCode.OK);
        await Assert.That(generation).Contains("\"type\":\"ready_for_moderation\"");
        using var http = CreateAuthenticatedHttpClient(telegramUserId);
        using var moderation = await http.PostAsync(
            $"/posts/publications/{publicationId}/moderation",
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
}
