using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs;
using Reki.Tests.Api;
using TickerQ.Exceptions;
using TickerQ.Utilities;
using TickerQ.Utilities.Base;

namespace Reki.Tests.Jobs;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class StarFeedJobsTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Generate_RepeatedExecution_PublishesOneStarPostWithoutChangingEconomyAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(9501);
        var player = await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var before = await GetStoredPlayerAsync(9501);
        var worldBefore = await GetStoredWorldAsync(9501);
        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Сегодня репетировали новую сцену.")));
        var context = new TickerFunctionContext<StarFeedRequest>(
            new TickerFunctionContext(),
            new StarFeedRequest(PlayerId.From(player.Id), WorldId.From(worldId), PostId.From(ObjectId.GenerateNewId().ToString()), "post", "star-feed-success"));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedPostJob>();

        // Act
        await job.ExecuteAsync(context, CancellationToken.None);
        await job.ExecuteAsync(context, CancellationToken.None);
        var feed = await api.Posts.GetFeedAsync(worldId, CancellationToken.None);

        // Assert
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(1);
        var lastRequest = Factory.Llm.GetLastRequest();
        await Assert.That(lastRequest.TryGetProperty("stream", out var streaming) && streaming.GetBoolean()).IsFalse();
        await Assert.That(feed.Count).IsEqualTo(1);
        await Assert.That(feed.Single().Text).IsEqualTo("Сегодня репетировали новую сцену.");
        var stored = await Database.GetCollection<GamePost>("posts").Find(FilterDefinition<GamePost>.Empty).SingleAsync();
        await Assert.That(ObjectId.TryParse(stored.Id.Value, out _)).IsTrue();
        await Assert.That(stored.AuthorId.Value.StartsWith("Star_", StringComparison.Ordinal)).IsTrue();
        await Assert.That((await GetStoredPlayerAsync(9501)).ToJson()).IsEqualTo(before.ToJson());
        await Assert.That((await GetStoredWorldAsync(9501)).ToJson()).IsEqualTo(worldBefore.ToJson());
        var detail = await api.Posts.GetPostAsync(stored.Id.Value, worldId, CancellationToken.None);
        await Assert.That(detail.Text).IsEqualTo(stored.Text);
    }

    [Test]
    [Arguments("")]
    [Arguments("порно")]
    public async Task Generate_InvalidContent_SkipsWithoutPublishingAsync(string text)
    {
        // Arrange
        using var api = Factory.CreateApiClient(9502);
        var player = await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
        var context = new TickerFunctionContext<StarFeedRequest>(
            new TickerFunctionContext(),
            new StarFeedRequest(PlayerId.From(player.Id), WorldId.From(worldId), PostId.From(ObjectId.GenerateNewId().ToString()), "post", "star-feed-invalid"));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedPostJob>();

        // Act
        await Assert.That(() => job.ExecuteAsync(context, CancellationToken.None)).Throws<TerminateExecutionException>();

        // Assert
        await Assert.That(await Database.GetCollection<GamePost>("posts").CountDocumentsAsync(FilterDefinition<GamePost>.Empty)).IsEqualTo(0);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(1);
    }

    [Test]
    public async Task Generate_OtherPlayersWorld_SkipsBeforeCallingAiAsync()
    {
        // Arrange
        using var first = Factory.CreateApiClient(9503);
        using var second = Factory.CreateApiClient(9504);
        var player = await CreatePlayerWithWorldAsync(first);
        await CreatePlayerWithWorldAsync(second);
        var context = new TickerFunctionContext<StarFeedRequest>(
            new TickerFunctionContext(),
            new StarFeedRequest(PlayerId.From(player.Id), WorldId.From(await GetWorldIdAsync(second)), PostId.From(ObjectId.GenerateNewId().ToString()), "post", "star-feed-foreign"));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedPostJob>();

        // Act
        await Assert.That(() => job.ExecuteAsync(context, CancellationToken.None)).Throws<TerminateExecutionException>();

        // Assert
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(0);
        await Assert.That(await Database.GetCollection<GamePost>("posts").CountDocumentsAsync(FilterDefinition<GamePost>.Empty)).IsEqualTo(0);
    }

    [Test]
    public async Task Publish_SuccessfulPost_PersistsTimeTickerWithRetriesAsync()
    {
        // Arrange
        const long user = 9505;
        using var api = Factory.CreateApiClient(user);
        await CreatePlayerWithWorldAsync(api);
        var tickers = Database.GetCollection<RekiTimeTicker>("ticker_TimeTickers");
        var existingIds = await tickers.Find(FilterDefinition<RekiTimeTicker>.Empty).Project(ticker => ticker.Id).ToListAsync();

        // Act
        await PublishAsync(user, "Новая история про сцену.");

        // Assert
        var scheduled = await tickers.Find(ticker => !existingIds.Contains(ticker.Id)).SingleAsync();
        var payload = TickerHelper.ReadTickerRequest<StarFeedRequest>(scheduled.Request);
        await Assert.That(payload.PlayerId).IsEqualTo(PlayerId.From((await GetStoredPlayerAsync(user))["_id"].AsString));
        await Assert.That(payload.WorldId).IsEqualTo(WorldId.From(await GetWorldIdAsync(api)));
        await Assert.That(ObjectId.TryParse(payload.PostId.Value, out _)).IsTrue();
        await Assert.That(scheduled.Function).IsEqualTo(nameof(GenerateStarFeedPostJob));
        await Assert.That(scheduled.Retries).IsEqualTo(3);
        await Assert.That(scheduled.RetryIntervals).IsEquivalentTo(new[] { 30, 120, 600 });
        await Assert.That(scheduled.ExecutionTime).IsEqualTo(Factory.Session.Clock.GetUtcNow().UtcDateTime);
        await Assert.That((await GetStoredPlayerAsync(user))["Energy"].AsInt32).IsEqualTo(9);
    }

    [Test]
    public async Task Generate_AfterTechnicalFailure_CanPublishOnLaterAttemptAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(9506);
        var player = await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var context = new TickerFunctionContext<StarFeedRequest>(
            new TickerFunctionContext(),
            new StarFeedRequest(PlayerId.From(player.Id), WorldId.From(worldId), PostId.From(ObjectId.GenerateNewId().ToString()), "post", "star-feed-retry"));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedPostJob>();
        Factory.Llm.RespondWithHttpError();
        Exception? failure = null;

        // Act
        try
        {
            await job.ExecuteAsync(context, CancellationToken.None);
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        // Assert
        await Assert.That(failure).IsNotNull();
        await Assert.That(failure is TerminateExecutionException).IsFalse();
        await Assert.That(await Database.GetCollection<GamePost>("posts").CountDocumentsAsync(FilterDefinition<GamePost>.Empty)).IsEqualTo(0);

        // Act
        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Новый день в мастерской.")));
        await job.ExecuteAsync(context, CancellationToken.None);

        // Assert
        var feed = await api.Posts.GetFeedAsync(worldId, CancellationToken.None);
        await Assert.That(feed.Single().PostId).IsEqualTo(context.Request.PostId.Value);
        await Assert.That(feed.Single().Text).IsEqualTo("Новый день в мастерской.");
    }

    [Test]
    public async Task Reply_ReplayedSuccessfulReply_SchedulesOnlyOneStarPostAsync()
    {
        // Arrange
        const long user = 9507;
        var (postId, commentId) = await PublishPostForThreadAsync(user);
        var tickers = Database.GetCollection<RekiTimeTicker>("ticker_TimeTickers");
        var existingIds = await tickers.Find(FilterDefinition<RekiTimeTicker>.Empty).Project(ticker => ticker.Id).ToListAsync();

        // Act
        await ReplyAsync(user, postId, commentId, "star-feed-reply", "Продолжение истории.");
        await ReplyAsync(user, postId, commentId, "star-feed-reply", "Продолжение истории.");

        // Assert
        var scheduled = await tickers.Find(ticker => !existingIds.Contains(ticker.Id)).SingleAsync();
        var payload = TickerHelper.ReadTickerRequest<StarFeedRequest>(scheduled.Request);
        await Assert.That(payload.Trigger).IsEqualTo("reply");
        await Assert.That(payload.TriggerId).IsEqualTo("star-feed-reply");
        await Assert.That(scheduled.Retries).IsEqualTo(3);
    }

    [Test]
    [Arguments(0, 0)]
    [Arguments(1, 1)]
    public async Task Feed_ConfiguredProbability_SchedulesExpectedActivityAsync(double probability, int expectedJobs)
    {
        // Arrange
        await using var apiFactory = new ApiFactory { Session = Factory.Session, FeedOpenProbability = probability };
        using var api = apiFactory.CreateApiClient(9508);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var tickers = Database.GetCollection<RekiTimeTicker>("ticker_TimeTickers");
        var existingIds = await tickers.Find(FilterDefinition<RekiTimeTicker>.Empty).Project(ticker => ticker.Id).ToListAsync();

        // Act
        var feed = await api.Posts.GetFeedAsync(worldId, CancellationToken.None);

        // Assert
        await Assert.That(feed.Count).IsEqualTo(0);
        var scheduled = await tickers.Find(ticker => !existingIds.Contains(ticker.Id)).ToListAsync();
        await Assert.That(scheduled.Count).IsEqualTo(expectedJobs);
        if (expectedJobs > 0)
        {
            var payload = TickerHelper.ReadTickerRequest<StarFeedRequest>(scheduled.Single().Request);
            await Assert.That(payload.Trigger).IsEqualTo("feed");
            await Assert.That(payload.WorldId).IsEqualTo(WorldId.From(worldId));
        }
    }
}
