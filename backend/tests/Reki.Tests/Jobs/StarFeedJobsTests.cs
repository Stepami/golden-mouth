using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Jobs;
using Reki.Infrastructure.Jobs.GenerateStarFeedOnCommentPublished;
using Reki.Infrastructure.Jobs.GenerateStarFeedOnFeedOpened;
using Reki.Infrastructure.Jobs.GenerateStarFeedOnPostPublished;
using Reki.Infrastructure.Jobs.StarFeed;
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
        var createdAt = Factory.Session.Clock.GetUtcNow().AddMilliseconds(123);
        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Сегодня репетировали новую сцену.")));
        var context = new TickerFunctionContext<StarFeedRequest>(
            new TickerFunctionContext(),
            new StarFeedRequest(player.Id, WorldId.From(worldId), PostId.New(createdAt), createdAt.ToUnixTimeMilliseconds()));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnFeedOpenedJob>();

        // Act
        Factory.Session.Clock.Advance(TimeSpan.FromMinutes(5));
        await job.ExecuteAsync(context, CancellationToken.None);
        Factory.Session.Clock.Advance(TimeSpan.FromMinutes(10));
        await job.ExecuteAsync(context, CancellationToken.None);
        var feed = await api.Posts.GetFeedAsync(worldId, null, null, CancellationToken.None);

        // Assert
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(1);
        var lastRequest = Factory.Llm.GetLastRequest();
        await Assert.That(lastRequest.TryGetProperty("stream", out var streaming) && streaming.GetBoolean()).IsFalse();
        await Assert.That(feed.Items.Count).IsEqualTo(1);
        await Assert.That(feed.Items.Single().Text).IsEqualTo("Сегодня репетировали новую сцену.");
        var stored = await Database.GetCollection<GamePost>("posts").Find(FilterDefinition<GamePost>.Empty).SingleAsync();
        await Assert.That(stored.Id.Value.Version == 7).IsTrue();
        await Assert.That(stored.Id).IsEqualTo(context.Request.PostId);
        await Assert.That(stored.CreatedAtUtc).IsEqualTo(createdAt);
        await Assert.That(stored.AuthorId.Value.StartsWith("Star_", StringComparison.Ordinal)).IsTrue();
        await Assert.That((await GetStoredPlayerAsync(9501)).ToJson()).IsEqualTo(before.ToJson());
        await Assert.That((await GetStoredWorldAsync(9501)).ToJson()).IsEqualTo(worldBefore.ToJson());
        var detail = await api.Posts.GetPostAsync(stored.Id.Value.ToString("N"), worldId, CancellationToken.None);
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
            new StarFeedRequest(player.Id, WorldId.From(worldId), PostId.New(Factory.Session.Clock.GetUtcNow()), Factory.Session.Clock.GetUtcNow().ToUnixTimeMilliseconds()));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnFeedOpenedJob>();

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
            new StarFeedRequest(player.Id, WorldId.From(await GetWorldIdAsync(second)), PostId.New(Factory.Session.Clock.GetUtcNow()), Factory.Session.Clock.GetUtcNow().ToUnixTimeMilliseconds()));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnFeedOpenedJob>();

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
        var payload = TickerHelper.ReadTickerRequest<StarFeedOnPostPublishedRequest>(scheduled.Request);
        await Assert.That(payload.PlayerId).IsEqualTo(PlayerId.From((await GetStoredPlayerAsync(user))["_id"].AsBsonBinaryData.ToGuid().ToString("N")));
        await Assert.That(payload.WorldId).IsEqualTo(WorldId.From(await GetWorldIdAsync(api)));
        var source = await Factory.Services.GetRequiredService<IPostRepository>().GetPostAsync(payload.SourcePostId, payload.WorldId, CancellationToken.None);
        await Assert.That(source?.Text).IsEqualTo("Новая история про сцену.");
        await Assert.That(payload.PostId.Value.Version).IsEqualTo(7);
        await Assert.That(payload.CreatedAtUnixMilliseconds).IsEqualTo(Factory.Session.Clock.GetUtcNow().ToUnixTimeMilliseconds());
        await Assert.That(scheduled.Function).IsEqualTo(nameof(GenerateStarFeedOnPostPublishedJob));
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
            new StarFeedRequest(player.Id, WorldId.From(worldId), PostId.New(Factory.Session.Clock.GetUtcNow()), Factory.Session.Clock.GetUtcNow().ToUnixTimeMilliseconds()));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnFeedOpenedJob>();
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
        var feed = await api.Posts.GetFeedAsync(worldId, null, null, CancellationToken.None);
        await Assert.That(feed.Items.Single().PostId).IsEqualTo(context.Request.PostId);
        await Assert.That(feed.Items.Single().Text).IsEqualTo("Новый день в мастерской.");
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
        var payload = TickerHelper.ReadTickerRequest<StarFeedOnCommentPublishedRequest>(scheduled.Request);
        await Assert.That(payload.PlayerId).IsEqualTo(PlayerId.From((await GetStoredPlayerAsync(user))["_id"].AsBsonBinaryData.ToGuid().ToString("N")));
        await Assert.That(scheduled.Function).IsEqualTo(nameof(GenerateStarFeedOnCommentPublishedJob));
        var source = await Factory.Services.GetRequiredService<IPostRepository>().GetPostAsync(payload.SourcePostId, payload.WorldId, CancellationToken.None);
        await Assert.That(source?.Comments.Single(item => item.Id == payload.SourceCommentId).Text).IsEqualTo("Продолжение истории.");
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
        var feed = await api.Posts.GetFeedAsync(worldId, null, null, CancellationToken.None);

        // Assert
        await Assert.That(feed.Items.Count).IsEqualTo(0);
        var scheduled = await tickers.Find(ticker => !existingIds.Contains(ticker.Id)).ToListAsync();
        await Assert.That(scheduled.Count).IsEqualTo(expectedJobs);
        if (expectedJobs > 0)
        {
            var payload = TickerHelper.ReadTickerRequest<StarFeedRequest>(scheduled.Single().Request);
            await Assert.That(payload.WorldId).IsEqualTo(WorldId.From(worldId));
            await Assert.That(scheduled.Single().Function).IsEqualTo(nameof(GenerateStarFeedOnFeedOpenedJob));
        }
    }

    [Test]
    public async Task Generate_HistoryContext_DistinguishesOwnPostsAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(9509);
        var player = await CreatePlayerWithWorldAsync(api);
        var playerId = player.Id;
        var worldId = WorldId.From(await GetWorldIdAsync(api));
        var postRepository = Factory.Services.GetRequiredService<IPostRepository>();
        var worldRepository = Factory.Services.GetRequiredService<IWorldRepository>();
        var world = await worldRepository.GetWorldAsync(playerId, worldId, CancellationToken.None)
            ?? throw new InvalidOperationException("Missing test world.");
        var selectedStar = world.Stars[0];
        var otherStar = world.Stars[1];
        var authoredPosts = new[]
        {
            (selectedStar.Id.ToAuthorId(), "Моя история звезды."),
            (otherStar.Id.ToAuthorId(), "История другой звезды."),
            (playerId.ToAuthorId(), "История игрока."),
        };
        foreach (var (author, text) in authoredPosts)
        {
            await postRepository.InsertPostAsync(
                new GamePost(PostId.New(Factory.Session.Clock.GetUtcNow()), author, worldId, text, 0, [], Factory.Session.Clock.GetUtcNow(), 0, 0, 1, false, false),
                CancellationToken.None);
        }

        for (var index = 0; index < 25; index++)
        {
            await postRepository.InsertPostAsync(
                new GamePost(PostId.New(Factory.Session.Clock.GetUtcNow().AddMinutes(index + 1)), otherStar.Id.ToAuthorId(), worldId, $"Другая запись {index}", 0, [], Factory.Session.Clock.GetUtcNow(), 0, 0, 1, false, false),
                CancellationToken.None);
        }

        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Новая самостоятельная история.")));
        var context = new TickerFunctionContext<StarFeedRequest>(
            new TickerFunctionContext(),
            new StarFeedRequest(playerId, worldId, PostId.New(Factory.Session.Clock.GetUtcNow()), Factory.Session.Clock.GetUtcNow().ToUnixTimeMilliseconds()));
        using var scope = Factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnFeedOpenedJob>();

        // Act
        await job.ExecuteAsync(context, CancellationToken.None);

        // Assert
        var prompt = GetCapturedChatMessages().Single(message => message.Role == ChatRole.User && message.Text.StartsWith("Общая лента", StringComparison.Ordinal)).Text;
        const string marker = "DATA_JSON:";
        using var document = JsonDocument.Parse(prompt[(prompt.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..]);
        var history = document.RootElement.EnumerateArray().ToArray();
        await Assert.That(history.Length).IsEqualTo(10);
        await Assert.That(history.All(item => !item.GetProperty("isOwn").GetBoolean() && item.GetProperty("starId").GetString() == otherStar.Id.Value)).IsTrue();
        await Assert.That(history.Any(item => item.GetProperty("text").GetString() == "Моя история звезды.")).IsFalse();
        var ownPrompt = GetCapturedChatMessages().Single(message => message.Role == ChatRole.User && message.Text.StartsWith("Собственные публикации", StringComparison.Ordinal)).Text;
        using var ownDocument = JsonDocument.Parse(ownPrompt[(ownPrompt.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..]);
        var own = ownDocument.RootElement.EnumerateArray().Single();
        await Assert.That(own.GetProperty("text").GetString()).IsEqualTo("Моя история звезды.");
        await Assert.That(own.GetProperty("isOwn").GetBoolean()).IsTrue();
        await Assert.That(own.GetProperty("starId").GetString()).IsEqualTo(selectedStar.Id.Value);
        await Assert.That(own.GetProperty("author").GetString()).IsEqualTo(selectedStar.Name);
        var saved = await postRepository.GetPostAsync(context.Request.PostId, worldId, CancellationToken.None);
        await Assert.That(saved?.AuthorId).IsEqualTo(selectedStar.Id.ToAuthorId());
    }

    [Test]
    public async Task Generate_NormalizedOwnDuplicate_CompletesWithoutPublishingOrRegeneratingAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(9510);
        var player = await CreatePlayerWithWorldAsync(api);
        var worldId = WorldId.From(await GetWorldIdAsync(api));
        var world = await Factory.Services.GetRequiredService<IWorldRepository>().GetWorldAsync(player.Id, worldId, CancellationToken.None)
            ?? throw new InvalidOperationException("Missing test world.");
        var now = Factory.Session.Clock.GetUtcNow();
        var original = new GamePost(PostId.New(now), world.Stars[0].Id.ToAuthorId(), worldId, "Сегодня новая история.", 0, [], now, 0, 0, 1, false, false);
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertPostAsync(original, CancellationToken.None);
        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, "  СЕГОДНЯ \n новая\tистория.  ")));
        var request = new StarFeedRequest(player.Id, worldId, PostId.New(now), now.ToUnixTimeMilliseconds());
        using var scope = Factory.Services.CreateScope();

        // Act
        await scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnFeedOpenedJob>().ExecuteAsync(
            new TickerFunctionContext<StarFeedRequest>(new TickerFunctionContext(), request), CancellationToken.None);

        // Assert
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(1);
        await Assert.That(await repository.GetPostAsync(request.PostId, worldId, CancellationToken.None)).IsNull();
        await Assert.That((await repository.GetPostAsync(original.Id, worldId, CancellationToken.None))?.Text).IsEqualTo(original.Text);
        await Assert.That(await Database.GetCollection<GamePost>("posts").CountDocumentsAsync(FilterDefinition<GamePost>.Empty)).IsEqualTo(1);
    }

    [Test]
    [Arguments(StarFeedEventType.PostPublished)]
    [Arguments(StarFeedEventType.CommentPublished)]
    public async Task Generate_DelayedReaction_UsesExactSavedContextAsync(StarFeedEventType sourceEvent)
    {
        // Arrange
        using var api = Factory.CreateApiClient(9511);
        var player = await CreatePlayerWithWorldAsync(api);
        var worldId = WorldId.From(await GetWorldIdAsync(api));
        var world = await Factory.Services.GetRequiredService<IWorldRepository>().GetWorldAsync(player.Id, worldId, CancellationToken.None)
            ?? throw new InvalidOperationException("Missing test world.");
        var now = Factory.Session.Clock.GetUtcNow();
        var parent = new GameComment(CommentId.New(), null, null, world.Stars[0].Id.ToAuthorId(), "Какая деталь карты странная?", false, []);
        var comment = new GameComment(CommentId.New(), parent.Id, "source-reply", player.Id.ToAuthorId(), "На карте нет моста.", false, []);
        var answer = new GameComment(CommentId.New(), comment.Id, null, world.Stars[0].Id.ToAuthorId(), "Я проверю мост по старому плану.", false, []);
        var unrelated = new GameComment(CommentId.New(), null, null, world.Stars[1].Id.ToAuthorId(), "Другая ветка", false, []);
        var source = new GamePost(PostId.New(now), sourceEvent == StarFeedEventType.CommentPublished ? world.Stars[0].Id.ToAuthorId() : player.Id.ToAuthorId(), worldId, "Я нашёл старую карту.", 0, [parent, comment, answer, unrelated], now, 0, 0, 1, false, false);
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        await repository.InsertPostAsync(source, CancellationToken.None);
        var postId = PostId.New(now);
        Factory.Session.Clock.Advance(TimeSpan.FromHours(4));
        Factory.Llm.Respond(new ChatResponse(new ChatMessage(ChatRole.Assistant, "На новой карте я тоже не вижу мост.")));
        using var scope = Factory.Services.CreateScope();

        var before = await GetStoredPlayerAsync(9511);
        var worldBefore = await GetStoredWorldAsync(9511);

        // Act
        if (sourceEvent == StarFeedEventType.PostPublished)
        {
            var context = new TickerFunctionContext<StarFeedOnPostPublishedRequest>(
                new TickerFunctionContext(), new StarFeedOnPostPublishedRequest(player.Id, worldId, postId, now.ToUnixTimeMilliseconds(), source.Id));
            var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnPostPublishedJob>();
            await job.ExecuteAsync(context, CancellationToken.None);
            await job.ExecuteAsync(context, CancellationToken.None);
        }
        else
        {
            var context = new TickerFunctionContext<StarFeedOnCommentPublishedRequest>(
                new TickerFunctionContext(), new StarFeedOnCommentPublishedRequest(player.Id, worldId, postId, now.ToUnixTimeMilliseconds(), source.Id, comment.Id));
            var job = scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnCommentPublishedJob>();
            await job.ExecuteAsync(context, CancellationToken.None);
            await job.ExecuteAsync(context, CancellationToken.None);
        }

        // Assert
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(1);
        await Assert.That((await GetStoredPlayerAsync(9511)).ToJson()).IsEqualTo(before.ToJson());
        await Assert.That((await GetStoredWorldAsync(9511)).ToJson()).IsEqualTo(worldBefore.ToJson());
        var messages = GetCapturedChatMessages();
        var prompt = messages.Single(message => message.Role == ChatRole.User && message.Text.StartsWith("Исходный контент", StringComparison.Ordinal)).Text;
        const string marker = "DATA_JSON:";
        using var document = JsonDocument.Parse(prompt[(prompt.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..]);
        var content = document.RootElement;
        await Assert.That(content.GetProperty("text").GetString()).IsEqualTo(source.Text);
        await Assert.That(content.GetProperty("authorId").GetString()).IsEqualTo(source.AuthorId.Value);
        await Assert.That(messages[0].Text.Contains(sourceEvent == StarFeedEventType.PostPublished ? "исходный пост игрока" : "комментарий игрока", StringComparison.Ordinal)).IsTrue();
        if (sourceEvent == StarFeedEventType.CommentPublished)
        {
            await Assert.That(content.GetProperty("comment").GetProperty("text").GetString()).IsEqualTo(comment.Text);
            var branch = content.GetProperty("branch").EnumerateArray().ToArray();
            await Assert.That(branch.Length).IsEqualTo(2);
            await Assert.That(branch[0].GetProperty("text").GetString()).IsEqualTo(parent.Text);
            await Assert.That(branch[1].GetProperty("authorId").GetString()).IsEqualTo(player.Id.ToAuthorId().Value);
            var replies = content.GetProperty("existingReplies").EnumerateArray().ToArray();
            await Assert.That(replies.Single().GetProperty("text").GetString()).IsEqualTo(answer.Text);
            await Assert.That(prompt.Contains(unrelated.Text, StringComparison.Ordinal)).IsFalse();
        }

        var saved = await repository.GetPostAsync(postId, worldId, CancellationToken.None);
        await Assert.That(saved?.Text).IsEqualTo("На новой карте я тоже не вижу мост.");
        await Assert.That(saved?.CreatedAtUtc).IsEqualTo(now);
        await Assert.That(await Database.GetCollection<GamePost>("posts").CountDocumentsAsync(FilterDefinition<GamePost>.Empty)).IsEqualTo(2);
    }

    [Test]
    [Arguments("missing-post")]
    [Arguments("foreign-world")]
    [Arguments("foreign-author")]
    [Arguments("missing-comment")]
    [Arguments("hidden-comment")]
    [Arguments("foreign-comment")]
    public async Task Generate_UnavailableReactionSource_DoesNotCallAiOrPublishAsync(string scenario)
    {
        // Arrange
        using var api = Factory.CreateApiClient(9512);
        var player = await CreatePlayerWithWorldAsync(api);
        var worldId = WorldId.From(await GetWorldIdAsync(api));
        var world = await Factory.Services.GetRequiredService<IWorldRepository>().GetWorldAsync(player.Id, worldId, CancellationToken.None)
            ?? throw new InvalidOperationException("Missing test world.");
        var now = Factory.Session.Clock.GetUtcNow();
        var comment = new GameComment(
            CommentId.New(),
            null,
            null,
            scenario == "foreign-comment" ? world.Stars[0].Id.ToAuthorId() : player.Id.ToAuthorId(),
            "Источник",
            scenario == "hidden-comment",
            []);
        var post = new GamePost(PostId.New(now), scenario == "foreign-author" ? world.Stars[0].Id.ToAuthorId() : player.Id.ToAuthorId(), scenario == "foreign-world" ? WorldId.From(Guid.NewGuid()) : worldId, "Исходный пост", 0, [comment], now, 0, 0, 1, false, false);
        var repository = Factory.Services.GetRequiredService<IPostRepository>();
        if (scenario != "missing-post")
        {
            await repository.InsertPostAsync(post, CancellationToken.None);
        }

        var isComment = scenario.EndsWith("comment", StringComparison.Ordinal);
        var sourceCommentId = scenario == "missing-comment" ? CommentId.New() : comment.Id;
        var postId = PostId.New(now);
        using var scope = Factory.Services.CreateScope();

        // Act
        if (isComment)
        {
            await Assert.That(() => scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnCommentPublishedJob>().ExecuteAsync(
                new TickerFunctionContext<StarFeedOnCommentPublishedRequest>(new TickerFunctionContext(), new StarFeedOnCommentPublishedRequest(player.Id, worldId, postId, now.ToUnixTimeMilliseconds(), post.Id, sourceCommentId)), CancellationToken.None)).Throws<TerminateExecutionException>();
        }
        else
        {
            await Assert.That(() => scope.ServiceProvider.GetRequiredService<GenerateStarFeedOnPostPublishedJob>().ExecuteAsync(
                new TickerFunctionContext<StarFeedOnPostPublishedRequest>(new TickerFunctionContext(), new StarFeedOnPostPublishedRequest(player.Id, worldId, postId, now.ToUnixTimeMilliseconds(), post.Id)), CancellationToken.None)).Throws<TerminateExecutionException>();
        }

        // Assert
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(0);
        await Assert.That(await repository.GetPostAsync(postId, worldId, CancellationToken.None)).IsNull();
    }
}
