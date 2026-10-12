using System.Net;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Reki.Api.Serialization;
using Reki.Application.Common;
using Reki.Application.Common.Safety;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Application.Worlds;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class ReplyTransactionTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    [Arguments("post")]
    [Arguments("player")]
    [Arguments("world")]
    [Arguments("request")]
    public async Task TryCommitReply_StaleState_RollsBackEveryDocumentAsync(string conflict)
    {
        // Arrange
        const long user = 8730;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var player = await ReadPlayerAsync(user);
        var world = await ReadWorldAsync((await GetStoredPostAsync(postId))["WorldId"].AsString);
        var post = await ReadPostAsync(postId);
        const string requestId = "transaction-key";
        if (conflict == "request")
        {
            post = post.AddComments([
                new GameComment(CommentId.New(), CommentId.From(targetId), requestId, player.Id.ToAuthorId(), "Сохранённый ответ", false, [])
            ]);
            await Database.GetCollection<GamePost>("posts").ReplaceOneAsync(item => item.Id == post.Id, post);
        }

        var beforePost = await GetStoredPostAsync(postId);
        var beforePlayer = await GetStoredPlayerAsync(user);
        var beforeWorld = await GetStoredWorldAsync(user);
        var updatedPost = post with { Text = "Нельзя сохранить", Version = post.Version + 1 };
        var updatedPlayer = player.SpendEnergy(1, 10);
        var updatedWorld = world.AwardThreadBonus(9);
        var store = Factory.Services.GetRequiredService<IGameStore>();

        // Act
        var committed = await store.TryCommitReplyAsync(
            updatedPost,
            post.Version + (conflict == "post" ? 1 : 0),
            requestId,
            updatedPlayer,
            player.Version + (conflict == "player" ? 1 : 0),
            updatedWorld,
            world.Version + (conflict == "world" ? 1 : 0),
            CancellationToken.None);

        // Assert
        await Assert.That(committed).IsFalse();
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(beforePost);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(beforePlayer);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(beforeWorld);
    }

    [Test]
    public async Task AddReply_ConcurrentSameRequest_CommitsExactlyOnceAsync()
    {
        // Arrange
        const long user = 8731;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var beforePlayer = await GetStoredPlayerAsync(user);
        var beforeWorld = await GetStoredWorldAsync(user);

        // Act
        var responses = await Task.WhenAll(
            ReplyAsync(user, postId, targetId, "shared-key", "Один ответ"),
            ReplyAsync(user, postId, targetId, "shared-key", "Один ответ"));
        var saved = await GetStoredPostAsync(postId);

        // Assert
        await Assert.That(saved["Comments"].AsBsonArray.Count).IsEqualTo(3);
        await Assert.That(saved["Comments"].AsBsonArray.Count(comment => comment["RequestId"] == "shared-key"))
            .IsEqualTo(1);
        await Assert.That(saved["Version"].AsInt32).IsEqualTo(1);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32)
            .IsEqualTo(beforePlayer["EnergyUsed"].AsInt32 + 1);
        var bonus = Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value.Thread.SubscriberBonus;
        await Assert.That((await GetStoredWorldAsync(user))["Player"]["Subscribers"].ToInt64())
            .IsEqualTo(beforeWorld["Player"]["Subscribers"].ToInt64() + bonus);
        await Assert.That(responses.Select(response => response.EnergyLeft)).IsEquivalentTo(new int?[] { 8, 8 });
        await Assert.That(responses.Select(response => response.Subscribers)).IsEquivalentTo(new long?[] { beforeWorld["Player"]["Subscribers"].ToInt64() + bonus, beforeWorld["Player"]["Subscribers"].ToInt64() + bonus });
        await Assert
            .That((responses[0].Post ?? throw new InvalidOperationException("Missing post.")).Comments.Select(comment =>
                comment.Id))
            .IsEquivalentTo(
                (responses[1].Post ?? throw new InvalidOperationException("Missing post.")).Comments.Select(comment =>
                    comment.Id));
    }

    [Test]
    public async Task AddReply_ConcurrentWorldsLastEnergy_CommitsOneCompleteTurnAsync()
    {
        // Arrange
        const long user = 8732;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        using var api = Factory.CreateApiClient(user);
        var secondWorld =
            await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var original = await ReadPostAsync(postId);
        var secondPost = original with { Id = PostId.New(), WorldId = WorldId.From(secondWorld.Id), Version = 0 };
        await Database.GetCollection<GamePost>("posts").InsertOneAsync(secondPost);
        var player = await ReadPlayerAsync(user);
        await Database.GetCollection<PlayerProfile>("players")
            .ReplaceOneAsync(item => item.Id == player.Id, player with { EnergyUsed = 9 });
        var worldsBefore = await Database.GetCollection<BsonDocument>("worlds")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        // Act
        var responses = await Task.WhenAll(
            SendReplyAsync(user, original, targetId, "first-key"),
            SendReplyAsync(user, secondPost, targetId, "second-key"));
        var savedPosts = await Database.GetCollection<BsonDocument>("posts").Find(FilterDefinition<BsonDocument>.Empty)
            .ToListAsync();
        var savedWorlds = await Database.GetCollection<BsonDocument>("worlds")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        // Assert
        await Assert.That(responses.Count(response =>
                response.Status == HttpStatusCode.OK && ParseReplyEvents(response.Body)[^1].Status == "completed"))
            .IsEqualTo(1);
        await Assert.That(savedPosts.Select(post => post["Comments"].AsBsonArray.Count).Order())
            .IsEquivalentTo(new[] { 1, 3 });
        await Assert.That(savedPosts.Sum(post => post["Version"].AsInt32)).IsEqualTo(1);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(10);
        var bonus = Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value.Thread.SubscriberBonus;
        await Assert
            .That(savedWorlds.Sum(world => world["Player"]["Subscribers"].ToInt64()) -
                  worldsBefore.Sum(world => world["Player"]["Subscribers"].ToInt64())).IsEqualTo((long)bonus);
        var loser = savedPosts.Single(post => post["Comments"].AsBsonArray.Count == 1);
        await Assert.That(savedWorlds.Single(world => world["_id"] == loser["WorldId"]))
            .IsEqualTo(worldsBefore.Single(world => world["_id"] == loser["WorldId"]));
    }

    [Test]
    [Arguments("cancel")]
    [Arguments("dispose")]
    [Arguments("energy")]
    public async Task Handle_StoppedAtPreview_DoesNotPublishOrAwardBonusAsync(string stop)
    {
        // Arrange
        const long user = 8733;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var beforePost = await GetStoredPostAsync(postId);
        var beforeWorld = await GetStoredWorldAsync(user);
        using var cancellation = new CancellationTokenSource();
        using var scope = Factory.Services.CreateScope();
        await using var updates = CreateHandler(user, scope.ServiceProvider)
            .Handle(CreateCommand(beforePost, targetId, "preview-key"), cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);
        await MoveToPreviewAsync(updates);
        var beforePlayer = await GetStoredPlayerAsync(user);

        // Act
        if (stop == "cancel")
        {
            await cancellation.CancelAsync();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await updates.MoveNextAsync());
        }
        else if (stop == "energy")
        {
            await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
                new BsonDocument("_id", beforePlayer["_id"]),
                Builders<BsonDocument>.Update.Set("EnergyUsed", 10).Inc("Version", 1));
            beforePlayer = await GetStoredPlayerAsync(user);
            await Assert.That(await updates.MoveNextAsync()).IsTrue();
            await Assert.That(updates.Current.Status).IsEqualTo("failed");
            await Assert.That(updates.Current.Code).IsEqualTo("request_conflict");
        }
        else
        {
            await updates.DisposeAsync();
        }

        // Assert
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(beforePost);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(beforePlayer);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(beforeWorld);
    }

    [Test]
    public async Task Handle_LostCompletedDelivery_GetReplayRecoversWithoutChargeAsync()
    {
        // Arrange
        const long user = 8734;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var post = await GetStoredPostAsync(postId);
        using var scope = Factory.Services.CreateScope();
        await using var updates = CreateHandler(user, scope.ServiceProvider)
            .Handle(CreateCommand(post, targetId, "durable-key"), CancellationToken.None).GetAsyncEnumerator();
        await MoveToPreviewAsync(updates);

        // Act
        await Assert.That(await updates.MoveNextAsync()).IsTrue();
        await updates.DisposeAsync();
        var committedPlayer = await GetStoredPlayerAsync(user);
        var committedWorld = await GetStoredWorldAsync(user);
        var count = Factory.Llm.RequestCount;
        using var api = Factory.CreateApiClient(user);
        var recovered = await api.Posts.GetPostAsync(postId, post["WorldId"].AsString, CancellationToken.None);
        var replay = await ReplyAsync(user, postId, targetId, "durable-key", "Ответ игрока");

        // Assert
        await Assert.That(recovered.Comments.Count).IsEqualTo(3);
        await Assert.That(recovered.Comments.Count(comment => comment.RequestId == "durable-key")).IsEqualTo(1);
        await Assert
            .That((replay.Post ?? throw new InvalidOperationException("Missing recovered post.")).Comments
                .Select(comment => comment.Id)).IsEquivalentTo(recovered.Comments.Select(comment => comment.Id));
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(count);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(committedPlayer);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(committedWorld);
    }

    [Test]
    public async Task Handle_CompetingBonusReply_RecalculatesFromFreshSavedCountAsync()
    {
        // Arrange
        const long user = 8735;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var balance = Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value;
        var player = await ReadPlayerAsync(user);
        var post = await ReadPostAsync(postId);
        var earlier = Enumerable.Range(0, balance.Thread.BonusReplies - 1)
            .Select(index => new GameComment(CommentId.New(), CommentId.From(targetId), $"earlier-{index}", player.Id.ToAuthorId(), "Исторический ответ", false, [])).ToArray();
        post = post.AddComments(earlier);
        await Database.GetCollection<GamePost>("posts").ReplaceOneAsync(item => item.Id == post.Id, post);
        using var scope = Factory.Services.CreateScope();
        await using var updates = CreateHandler(user, scope.ServiceProvider)
            .Handle(
                new AddReplyCommand(post.Id, CommentId.From(targetId), "pending-key", "Ответ игрока", post.WorldId),
                CancellationToken.None).GetAsyncEnumerator();
        await MoveToPreviewAsync(updates);

        // Act
        await ReplyAsync(user, postId, targetId, "competing-key", "Ответ первым");
        var worldAfterCompetitor = await GetStoredWorldAsync(user);
        await Assert.That(await updates.MoveNextAsync()).IsTrue();

        // Assert
        await Assert.That(updates.Current.Status).IsEqualTo("completed");
        await Assert.That((await GetStoredPostAsync(postId))["Comments"].AsBsonArray
            .Count(comment => comment["RequestId"] == "pending-key")).IsEqualTo(1);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(3);
        await Assert.That((await GetStoredWorldAsync(user))["Player"]["Subscribers"])
            .IsEqualTo(worldAfterCompetitor["Player"]["Subscribers"]);
    }

    [Test]
    [Arguments("hidden")]
    [Arguments("kind")]
    [Arguments("http-error")]
    [Arguments("duplicate-turn")]
    public async Task AddReply_InvalidTargetOrGeneration_PreservesWholeStateAsync(string scenario)
    {
        // Arrange
        const long user = 8736;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var post = await ReadPostAsync(postId);
        if (scenario == "hidden" || scenario == "kind")
        {
            var target = post.Comments[0] with
            {
                Hidden = scenario == "hidden",
                AuthorId = scenario == "kind"
                    ? AuthorId.From((AuthorKind.Player, post.Comments[0].AuthorId.Value.Item2))
                    : post.Comments[0].AuthorId,
            };
            post = post with { Comments = [target] };
            await Database.GetCollection<GamePost>("posts").ReplaceOneAsync(item => item.Id == post.Id, post);
        }
        else if (scenario == "http-error")
        {
            Factory.Llm.RespondWithHttpError();
        }
        else
        {
            var response = CreateReplyTurnResponse(post.Comments[0].AuthorId.Value.Item2);
            var call = response.Messages[0].Contents.OfType<FunctionCallContent>().Single();
            response.Messages[0].Contents.Add(new FunctionCallContent("duplicate", call.Name, call.Arguments));
            Factory.Llm.Respond(response);
        }

        var beforePost = await GetStoredPostAsync(postId);
        var beforePlayer = await GetStoredPlayerAsync(user);
        var beforeWorld = await GetStoredWorldAsync(user);
        var count = Factory.Llm.RequestCount;

        // Act
        var result = await SendReplyAsync(user, post, targetId, "failure-key");

        // Assert
        if (scenario == "hidden" || scenario == "kind")
        {
            await Assert.That(result.Status)
                .IsEqualTo(scenario == "hidden" ? HttpStatusCode.NotFound : HttpStatusCode.Conflict);
            await Assert.That(Factory.Llm.RequestCount).IsEqualTo(count);
        }
        else
        {
            await Assert.That(result.Status).IsEqualTo(HttpStatusCode.OK);
            var events = ParseReplyEvents(result.Body);
            await Assert.That(events[^1].Status).IsEqualTo("failed");
            await Assert.That(events[^1].Code).IsEqualTo("ai_unavailable");
            await Assert.That(events.Any(item => item.Status == "completed" || item.Text is not null)).IsFalse();
        }

        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(beforePost);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(beforePlayer);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(beforeWorld);
    }

    [Test]
    public async Task AddReply_ConcurrentChangedPayload_SavesOnlyOnePayloadAsync()
    {
        // Arrange
        const long user = 8737;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var post = await ReadPostAsync(postId);
        var beforeWorld = await GetStoredWorldAsync(user);

        // Act
        var responses = await Task.WhenAll(
            SendReplyAsync(user, post, targetId, "contested-key", "Первая мысль"),
            SendReplyAsync(user, post, targetId, "contested-key", "Вторая мысль"));
        var saved = await ReadPostAsync(postId);

        // Assert
        await Assert.That(responses.Count(response =>
                response.Status == HttpStatusCode.OK && ParseReplyEvents(response.Body)[^1].Status == "completed"))
            .IsEqualTo(1);
        await Assert.That(saved.Comments.Count).IsEqualTo(3);
        var playerReply = saved.Comments.Single(comment => comment.RequestId == "contested-key");
        await Assert.That(new[] { "Первая мысль", "Вторая мысль" }).Contains(playerReply.Text);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(2);
        var bonus = Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value.Thread.SubscriberBonus;
        await Assert.That((await GetStoredWorldAsync(user))["Player"]["Subscribers"].ToInt64())
            .IsEqualTo(beforeWorld["Player"]["Subscribers"].ToInt64() + bonus);
    }

    [Test]
    public async Task Handle_ReplayWithStalePlayerSnapshot_ReturnsDurableCurrentBalanceAsync()
    {
        // Arrange
        const long user = 8738;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var stalePlayer = await ReadPlayerAsync(user);
        await ReplyAsync(user, postId, targetId, "snapshot-key", "Ответ игрока");
        var saved = await GetStoredPostAsync(postId);
        var world = await GetStoredWorldAsync(user);
        var count = Factory.Llm.RequestCount;
        using var scope = Factory.Services.CreateScope();
        var handler = BuildHandler(scope.ServiceProvider, () => ValueTask.FromResult(stalePlayer));
        await using var updates = handler.Handle(CreateCommand(saved, targetId, "snapshot-key"), CancellationToken.None).GetAsyncEnumerator();

        // Act
        var moved = await updates.MoveNextAsync();

        // Assert
        await Assert.That(moved).IsTrue();
        await Assert.That(updates.Current.Status).IsEqualTo("completed");
        await Assert.That(updates.Current.EnergyLeft).IsEqualTo(8);
        await Assert.That(updates.Current.Subscribers).IsEqualTo(world["Player"]["Subscribers"].ToInt64());
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(count);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(2);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(saved);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(world);
    }

    [Test]
    public async Task Handle_TwoGeneratedSameKeys_CommitsOneDurableTurnAsync()
    {
        // Arrange
        const long user = 8739;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var beforePost = await GetStoredPostAsync(postId);
        var beforePlayer = await GetStoredPlayerAsync(user);
        var beforeWorld = await GetStoredWorldAsync(user);
        var count = Factory.Llm.RequestCount;
        using var firstScope = Factory.Services.CreateScope();
        using var secondScope = Factory.Services.CreateScope();
        var command = CreateCommand(beforePost, targetId, "generated-shared-key");
        await using var first = CreateHandler(user, firstScope.ServiceProvider).Handle(command, CancellationToken.None).GetAsyncEnumerator();
        await using var second = CreateHandler(user, secondScope.ServiceProvider).Handle(command, CancellationToken.None).GetAsyncEnumerator();
        await MoveToPreviewAsync(first);
        await MoveToPreviewAsync(second);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(count + 2);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(beforePost);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(beforePlayer);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(beforeWorld);

        // Act
        var outcomes = await Task.WhenAll(first.MoveNextAsync().AsTask(), second.MoveNextAsync().AsTask());
        var saved = await GetStoredPostAsync(postId);
        var world = await GetStoredWorldAsync(user);

        // Assert
        await Assert.That(outcomes).IsEquivalentTo(new[] { true, true });
        await Assert.That(first.Current.Status).IsEqualTo("completed");
        await Assert.That(second.Current.Status).IsEqualTo("completed");
        await Assert.That(first.Current.EnergyLeft).IsEqualTo(8);
        await Assert.That(second.Current.EnergyLeft).IsEqualTo(8);
        await Assert.That(first.Current.Subscribers).IsEqualTo(world["Player"]["Subscribers"].ToInt64());
        await Assert.That(second.Current.Subscribers).IsEqualTo(world["Player"]["Subscribers"].ToInt64());
        await Assert.That((first.Current.Post ?? throw new InvalidOperationException("Missing first post.")).Comments.Select(comment => comment.Id))
            .IsEquivalentTo((second.Current.Post ?? throw new InvalidOperationException("Missing second post.")).Comments.Select(comment => comment.Id));
        await Assert.That(saved["Version"].AsInt32).IsEqualTo(beforePost["Version"].AsInt32 + 1);
        await Assert.That(saved["Comments"].AsBsonArray.Count).IsEqualTo(3);
        await Assert.That(saved["Comments"].AsBsonArray.Count(comment => comment["RequestId"] == "generated-shared-key")).IsEqualTo(1);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(beforePlayer["EnergyUsed"].AsInt32 + 1);
        var bonus = Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value.Thread.SubscriberBonus;
        await Assert.That(world["Player"]["Subscribers"].ToInt64()).IsEqualTo(beforeWorld["Player"]["Subscribers"].ToInt64() + bonus);
    }

    [Test]
    public async Task Handle_TwoGeneratedWorldsLastEnergy_CommitsOnlyFirstTurnAsync()
    {
        // Arrange
        const long user = 8740;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        using var api = Factory.CreateApiClient(user);
        var secondWorld = await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var original = await ReadPostAsync(postId);
        var secondPost = original with { Id = PostId.New(), WorldId = WorldId.From(secondWorld.Id), Version = 0 };
        await Database.GetCollection<GamePost>("posts").InsertOneAsync(secondPost);
        var player = await ReadPlayerAsync(user);
        await Database.GetCollection<PlayerProfile>("players").ReplaceOneAsync(item => item.Id == player.Id, player with { EnergyUsed = 9 });
        var beforeFirstPost = await GetStoredPostAsync(postId);
        var beforeSecondPost = await GetStoredPostAsync(secondPost.Id.Value);
        var beforePlayer = await GetStoredPlayerAsync(user);
        var beforeFirstWorld = await ReadWorldDocumentAsync(original.WorldId.Value);
        var beforeSecondWorld = await ReadWorldDocumentAsync(secondPost.WorldId.Value);
        var count = Factory.Llm.RequestCount;
        using var firstScope = Factory.Services.CreateScope();
        using var secondScope = Factory.Services.CreateScope();
        await using var first = CreateHandler(user, firstScope.ServiceProvider).Handle(CreateCommand(beforeFirstPost, targetId, "generated-first"), CancellationToken.None).GetAsyncEnumerator();
        await using var second = CreateHandler(user, secondScope.ServiceProvider).Handle(CreateCommand(beforeSecondPost, targetId, "generated-second"), CancellationToken.None).GetAsyncEnumerator();
        await MoveToPreviewAsync(first);
        await MoveToPreviewAsync(second);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(count + 2);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(beforeFirstPost);
        await Assert.That(await GetStoredPostAsync(secondPost.Id.Value)).IsEqualTo(beforeSecondPost);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(beforePlayer);
        await Assert.That(await ReadWorldDocumentAsync(original.WorldId.Value)).IsEqualTo(beforeFirstWorld);
        await Assert.That(await ReadWorldDocumentAsync(secondPost.WorldId.Value)).IsEqualTo(beforeSecondWorld);

        // Act
        var firstMoved = await first.MoveNextAsync();
        var secondMoved = await second.MoveNextAsync();
        var saved = await GetStoredPostAsync(postId);
        var world = await ReadWorldDocumentAsync(original.WorldId.Value);

        // Assert
        await Assert.That(firstMoved).IsTrue();
        await Assert.That(secondMoved).IsTrue();
        await Assert.That(first.Current.Status).IsEqualTo("completed");
        await Assert.That(first.Current.EnergyLeft).IsEqualTo(0);
        await Assert.That(second.Current.Status).IsEqualTo("failed");
        await Assert.That(second.Current.Code).IsEqualTo("request_conflict");
        await Assert.That(saved["Version"].AsInt32).IsEqualTo(beforeFirstPost["Version"].AsInt32 + 1);
        await Assert.That(saved["Comments"].AsBsonArray.Count).IsEqualTo(3);
        await Assert.That((await GetStoredPlayerAsync(user))["EnergyUsed"].AsInt32).IsEqualTo(10);
        var bonus = Factory.Services.GetRequiredService<IOptions<GameBalance>>().Value.Thread.SubscriberBonus;
        await Assert.That(world["Player"]["Subscribers"].ToInt64()).IsEqualTo(beforeFirstWorld["Player"]["Subscribers"].ToInt64() + bonus);
        await Assert.That(await GetStoredPostAsync(secondPost.Id.Value)).IsEqualTo(beforeSecondPost);
        await Assert.That(await ReadWorldDocumentAsync(secondPost.WorldId.Value)).IsEqualTo(beforeSecondWorld);
    }

    [Test]
    [Arguments("selected")]
    [Arguments("extra")]
    public async Task Handle_AuthorChangedAfterPreview_PreservesConcurrentStateAsync(string scenario)
    {
        // Arrange
        const long user = 8741;
        var (postId, targetId) = await PublishPostForThreadAsync(user);
        var post = await ReadPostAsync(postId);
        var world = await ReadWorldAsync(post.WorldId.Value);
        var selected = post.Comments[0].AuthorId.Value.Item2;
        var other = world.Stars.First(star => star.Id.Value != selected);
        Factory.Llm.Respond(CreateReplyTurnResponse(selected, arguments =>
        {
            if (scenario == "extra")
            {
                arguments["additional_star_id"] = other.Id.Value;
                arguments["additional_text"] = "Есть ещё один взгляд на тему.";
                arguments["additional_tone"] = "critique";
            }
        }));
        using var scope = Factory.Services.CreateScope();
        var command = new AddReplyCommand(post.Id, CommentId.From(targetId), "changed-author", "Ответ игрока", world.Id);
        await using var updates = CreateHandler(user, scope.ServiceProvider).Handle(command, CancellationToken.None).GetAsyncEnumerator();
        await MoveToPreviewAsync(updates);
        if (scenario == "selected")
        {
            var changed = post with { Comments = [post.Comments[0] with { AuthorId = other.Id.ToAuthorId() }], Version = post.Version + 1 };
            await Database.GetCollection<GamePost>("posts").ReplaceOneAsync(item => item.Id == post.Id, changed);
        }
        else
        {
            var changed = world with { Stars = world.Stars.Where(star => star.Id != other.Id).ToArray(), Version = world.Version + 1 };
            await Database.GetCollection<World>("worlds").ReplaceOneAsync(item => item.Id == world.Id, changed);
        }

        var concurrentPost = await GetStoredPostAsync(postId);
        var concurrentPlayer = await GetStoredPlayerAsync(user);
        var concurrentWorld = await ReadWorldDocumentAsync(world.Id.Value);
        var count = Factory.Llm.RequestCount;

        // Act
        var moved = await updates.MoveNextAsync();

        // Assert
        await Assert.That(moved).IsTrue();
        await Assert.That(updates.Current.Status).IsEqualTo("failed");
        await Assert.That(updates.Current.Code).IsEqualTo("request_conflict");
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(count);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(concurrentPost);
        await Assert.That(await GetStoredPlayerAsync(user)).IsEqualTo(concurrentPlayer);
        await Assert.That(await ReadWorldDocumentAsync(world.Id.Value)).IsEqualTo(concurrentWorld);
    }

    private static AddReplyCommand CreateCommand(BsonDocument post, string targetId, string key) =>
        new(PostId.From(post["_id"].AsString), CommentId.From(targetId), key, "Ответ игрока", WorldId.From(post["WorldId"].AsString));

    private static async Task MoveToPreviewAsync(IAsyncEnumerator<ReplyLiveEvent> updates)
    {
        while (await updates.MoveNextAsync())
        {
            if (updates.Current.Status == "generating" && updates.Current.Text is not null)
            {
                return;
            }
        }

        throw new InvalidOperationException("No unpublished preview was generated.");
    }

    private AddReplyStreamHandler CreateHandler(long user, IServiceProvider services) =>
        BuildHandler(services, () => new ValueTask<PlayerProfile>(ReadPlayerAsync(user)));

    private AddReplyStreamHandler BuildHandler(IServiceProvider services, Func<ValueTask<PlayerProfile>> readPlayer)
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetPlayerQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => readPlayer());
        sender.Send(Arg.Any<GetWorldQuery>(), Arg.Any<CancellationToken>()).Returns(call =>
            new ValueTask<World>(ReadWorldAsync(call.Arg<GetWorldQuery>().WorldId.Value)));
        sender.Send(Arg.Any<GetPostQuery>(), Arg.Any<CancellationToken>()).Returns(call =>
            new ValueTask<GamePost>(ReadPostAsync(call.Arg<GetPostQuery>().PostId.Value)));

        return new AddReplyStreamHandler(
            services.GetRequiredService<IGameStore>(),
            services.GetRequiredService<IOptions<GameBalance>>(),
            sender,
            services.GetRequiredService<GameDay>(),
            Factory.Session.Clock,
            services.GetRequiredService<PlayerSafetyGuard>(),
            services.GetRequiredService<StarTurnGenerator>(),
            services.GetRequiredService<ILogger<AddReplyStreamHandler>>());
    }

    private Task<PlayerProfile> ReadPlayerAsync(long user) =>
        Database.GetCollection<PlayerProfile>("players").Find(item => item.TelegramUserId == TelegramUserId.From(user))
            .SingleAsync();

    private Task<GamePost> ReadPostAsync(string id) =>
        Database.GetCollection<GamePost>("posts").Find(item => item.Id == PostId.From(id)).SingleAsync();

    private Task<World> ReadWorldAsync(string id) =>
        Database.GetCollection<World>("worlds").Find(item => item.Id == WorldId.From(id)).SingleAsync();

    private Task<BsonDocument> ReadWorldDocumentAsync(string id) =>
        Database.GetCollection<BsonDocument>("worlds").Find(new BsonDocument("_id", id)).SingleAsync();

    private async Task<(HttpStatusCode Status, string Body)> SendReplyAsync(long user, GamePost post, string target, string key, string text = "Ответ игрока")
    {
        using var http = CreateAuthenticatedHttpClient(user);
        var payload = new ReplyRequest { CommentId = target, RequestId = key, Text = text };
        using var body =
            new StringContent(JsonSerializer.Serialize(payload, ApiJsonSerializerContext.Default.ReplyRequest), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync($"/posts/{post.Id.Value}/replies?worldId={post.WorldId.Value}", body, CancellationToken.None);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(CancellationToken.None));
    }
}