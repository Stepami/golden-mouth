using MongoDB.Driver;
using Reki.Api.Requests.Posts;
using Reki.Application.Player.Responses;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.Reply;
using Reki.Domain.Bonuses;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class DailyChallengeProgressTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task CreatePost_Thursday_TracksPublicationProgressAsync()
    {
        // Arrange
        Factory.Session.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));
        using var api = Factory.CreateApiClient(8931);
        var player = await CreatePlayerWithWorldAsync(api);
        await Database.GetCollection<Bonus>("bonuses").OfType<FlopGuardBonus>()
            .DeleteOneAsync(bonus => bonus.PlayerId == player.Id);
        var worldId = WorldId.From(await GetWorldIdAsync(api));

        // Act
        var first = await PublishAsync(api, worldId, "Сегодня мы готовимся к концерту.");
        var afterFirst = await ReadChallengeAsync(api);
        var star = await ReplyAsync(api, worldId, first.Post.PostId, first.PredictedAuthorId);
        var reply = await ReplyAsync(api, worldId, first.Post.PostId, player.Id.ToAuthorId(), star.CommentId);
        var afterReply = await ReadChallengeAsync(api);
        var repost = await PublishAsync(api, worldId, string.Empty, first.Post.PostId);
        var afterRepost = await ReadChallengeAsync(api);
        var second = await PublishFlopAsync(api, worldId);
        var afterSecond = await ReadChallengeAsync(api);

        // Assert
        await Assert.That(first.Result.Delta).IsGreaterThan(0);
        await Assert.That(afterFirst.Values[worldId][DailyTaskType.Posts].Progress).IsEqualTo(1L);
        await Assert.That(afterFirst.Values[worldId][DailyTaskType.Subscribers].Progress).IsEqualTo(first.Result.Delta);
        await Assert.That(afterFirst.Values[worldId][DailyTaskType.PostLikes].Progress).IsEqualTo(first.Post.Likes);
        await Assert.That(afterSecond.Values[worldId][DailyTaskType.Posts].Progress).IsEqualTo(2L);
        await Assert.That(afterSecond.Values[worldId][DailyTaskType.PostLikes].Progress).IsEqualTo(Math.Max(first.Post.Likes, second.Post.Likes));
        await Assert.That(second.Result.Delta).IsLessThan(0);
        await Assert.That(reply.Subscribers).IsGreaterThan(star.Subscribers);
        await Assert.That(afterReply.Values[worldId][DailyTaskType.Subscribers].Progress).IsEqualTo(first.Result.Delta);
        await Assert.That(afterRepost.Values[worldId][DailyTaskType.Subscribers].Progress).IsEqualTo(first.Result.Delta + repost.Result.Delta);
        await Assert.That(afterRepost.Values[worldId][DailyTaskType.PostLikes].Progress).IsEqualTo(first.Post.Likes);
        await Assert.That(afterSecond.Values[worldId][DailyTaskType.Subscribers].Progress).IsEqualTo(first.Result.Delta + repost.Result.Delta);
    }

    [Test]
    public async Task CreatePost_Friday_KeepsProgressPerWorldWithSharedCompletionAsync()
    {
        // Arrange
        const long user = 8932;
        Factory.Session.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
        using var api = Factory.CreateApiClient(user);
        var player = await CreatePlayerWithWorldAsync(api);
        await Database.GetCollection<Bonus>("bonuses").DeleteManyAsync(bonus => bonus.PlayerId == player.Id);
        var firstWorld = WorldId.From(await GetWorldIdAsync(api));
        var secondWorld = (await api.Worlds.CreateWorldAsync(
            await CreateWorldDefinitionAsync(api, WorldTemplate.Academy), CancellationToken.None)).Id;
        var target = (await ReadChallengeAsync(api)).Values[firstWorld][DailyTaskType.Posts].Target;

        // Act
        var first = await PublishAsync(api, firstWorld, "Покажу афишу ближайшего концерта.");
        var repost = await PublishAsync(api, firstWorld, string.Empty, first.Post.PostId);
        await PublishAsync(api, secondWorld, "Начинаем новый семестр.");
        var split = await ReadChallengeAsync(api);
        var flop = await PublishFlopAsync(api, firstWorld);
        var afterFlop = await ReadChallengeAsync(api);
        for (var count = 2L; count < target; count++)
        {
            await PublishAsync(api, firstWorld, "Скоро объявим программу выступления.");
        }

        var completed = await ReadChallengeAsync(api);
        var repostTarget = completed.Values[firstWorld][DailyTaskType.Reposts].Target;
        for (var count = 1L; count < repostTarget - 1; count++)
        {
            await PublishAsync(api, firstWorld, string.Empty, first.Post.PostId);
        }

        var beforeFinish = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var priorPosts = await Database.GetCollection<GamePost>("posts")
            .CountDocumentsAsync(post => post.OwnerId == player.Id && post.AuthorId == player.Id.ToAuthorId());
        await PublishAsync(api, firstWorld, string.Empty, first.Post.PostId);
        var finishedCard = await ReadChallengeAsync(api);
        var finishedWallet = (await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).EnergyWallet?.Crystals;
        var nextPost = await PublishAsync(api, secondWorld, "Сегодня следующая репетиция.");
        var afterFinish = await ReadChallengeAsync(api);
        var afterFinishWallet = (await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).EnergyWallet?.Crystals;
        await api.Worlds.DeleteWorldAsync(firstWorld.ToString(), CancellationToken.None);
        var afterDelete = await ReadChallengeAsync(api);

        // Assert
        await Assert.That(repost.Result.Delta).IsGreaterThan(0);
        await Assert.That(split.Values[firstWorld][DailyTaskType.Posts].Progress).IsEqualTo(1L);
        await Assert.That(split.Values[firstWorld][DailyTaskType.Reposts].Progress).IsEqualTo(1L);
        await Assert.That(split.Values[firstWorld][DailyTaskType.Unsubscribers].Progress).IsEqualTo(0L);
        await Assert.That(split.Values[secondWorld][DailyTaskType.Posts].Progress).IsEqualTo(1L);
        await Assert.That(split.Values[secondWorld][DailyTaskType.Reposts].Progress).IsEqualTo(0L);
        await Assert.That(split.Completion[DailyTaskType.Posts]).IsFalse();
        await Assert.That(flop.Result.Delta).IsLessThan(0);
        await Assert.That(afterFlop.Values[firstWorld][DailyTaskType.Unsubscribers].Progress).IsEqualTo(-flop.Result.Delta);
        await Assert.That(completed.Values[firstWorld][DailyTaskType.Posts].Progress).IsEqualTo(target);
        await Assert.That(completed.Values[secondWorld][DailyTaskType.Posts].Progress).IsEqualTo(1L);
        await Assert.That(completed.Completion[DailyTaskType.Posts]).IsTrue();
        await Assert.That(finishedCard.Completion.Values.All(done => done)).IsTrue();
        await Assert.That(finishedCard.Values[firstWorld][DailyTaskType.Reposts].Progress).IsEqualTo(repostTarget);
        var publicationReward = (priorPosts + 1) % 5 == 0 ? 1 : 0;
        await Assert.That(finishedWallet - beforeFinish.EnergyWallet?.Crystals).IsEqualTo(publicationReward + 2);
        await Assert.That(afterFinishWallet - finishedWallet).IsEqualTo(nextPost.Result.Viral ? 1 : 0);
        await Assert.That(afterFinish.Values[secondWorld][DailyTaskType.Posts].Progress).IsEqualTo(1L);
        await Assert.That(afterDelete.Values.ContainsKey(firstWorld)).IsFalse();
        await Assert.That(afterDelete.Completion[DailyTaskType.Posts]).IsTrue();
    }

    [Test]
    public async Task CreatePost_Wednesday_TracksPeakViewsFromOnePublicationAsync()
    {
        // Arrange
        var clock = Factory.Session.Clock;
        clock.SetUtcNow(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
        using var api = Factory.CreateApiClient(8933);
        await CreatePlayerWithWorldAsync(api);
        var worldId = WorldId.From(await GetWorldIdAsync(api));
        var original = await PublishAsync(api, worldId, "Наш первый день на сцене.");
        clock.Advance(TimeSpan.FromDays(1));
        await ReadChallengeAsync(api);

        // Act
        await PublishAsync(api, worldId, "Вот с чего началась неделя.", original.Post.PostId);
        var afterRepost = await ReadChallengeAsync(api);
        var first = await PublishAsync(api, worldId, "Покажу костюмы для новой постановки.");
        var afterFirst = await ReadChallengeAsync(api);
        var second = await PublishAsync(api, worldId, "Завтра открытая репетиция.");
        var afterSecond = await ReadChallengeAsync(api);

        // Assert
        await Assert.That(afterRepost.Values[worldId][DailyTaskType.Reposts].Progress).IsEqualTo(1L);
        await Assert.That(afterRepost.Values[worldId][DailyTaskType.PostViews].Progress).IsEqualTo(0L);
        await Assert.That(afterFirst.Values[worldId][DailyTaskType.PostViews].Progress).IsEqualTo(first.Post.Views);
        await Assert.That(afterSecond.Values[worldId][DailyTaskType.PostViews].Progress).IsEqualTo(Math.Max(first.Post.Views, second.Post.Views));
    }

    [Test]
    public async Task ReplyToPost_Tuesday_TracksOnlyEligibleRepliesAsync()
    {
        // Arrange
        var clock = Factory.Session.Clock;
        clock.SetUtcNow(new DateTimeOffset(2026, 10, 5, 23, 59, 0, TimeSpan.Zero));
        using var api = Factory.CreateApiClient(8934);
        var player = await CreatePlayerWithWorldAsync(api);
        var world = (await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Single();
        var yesterday = await PublishAsync(api, world.Id, "До полуночи публикую расписание.");
        clock.Advance(TimeSpan.FromMinutes(1));
        var challenge = await ReadChallengeAsync(api);
        var selected = challenge.Values[world.Id][DailyTaskType.StarComment].StarId
            ?? throw new InvalidOperationException("The daily task has no selected star.");
        var other = world.Stars.First(star => star.Id != selected).Id;
        var today = await PublishAsync(api, world.Id, "Сегодня репетируем новую сцену.");
        var repost = await PublishAsync(api, world.Id, "Делюсь расписанием ещё раз.", yesterday.Post.PostId);

        // Act
        await ReplyAsync(api, world.Id, yesterday.Post.PostId, selected.ToAuthorId());
        var afterYesterday = await ReadChallengeAsync(api);
        await ReplyAsync(api, world.Id, repost.Post.PostId, selected.ToAuthorId());
        var afterRepost = await ReadChallengeAsync(api);
        var otherComment = await ReplyAsync(api, world.Id, today.Post.PostId, other.ToAuthorId());
        var afterOtherStar = await ReadChallengeAsync(api);
        await ReplyAsync(api, world.Id, today.Post.PostId, player.Id.ToAuthorId(), otherComment.CommentId);
        var afterPlayer = await ReadChallengeAsync(api);
        await ReplyAsync(api, world.Id, today.Post.PostId, selected.ToAuthorId());
        var afterSelectedStar = await ReadChallengeAsync(api);

        // Assert
        await Assert.That(afterYesterday.Values[world.Id][DailyTaskType.StarComment].Progress).IsEqualTo(0L);
        await Assert.That(afterRepost.Values[world.Id][DailyTaskType.StarComment].Progress).IsEqualTo(0L);
        await Assert.That(afterOtherStar.Values[world.Id][DailyTaskType.StarComment].Progress).IsEqualTo(0L);
        await Assert.That(afterOtherStar.Values[world.Id][DailyTaskType.Comments].Progress).IsEqualTo(0L);
        await Assert.That(afterPlayer.Values[world.Id][DailyTaskType.Comments].Progress).IsEqualTo(1L);
        await Assert.That(afterPlayer.Completion[DailyTaskType.StarComment]).IsFalse();
        await Assert.That(afterSelectedStar.Values[world.Id][DailyTaskType.StarComment].Progress).IsEqualTo(1L);
        await Assert.That(afterSelectedStar.Completion[DailyTaskType.StarComment]).IsTrue();
        await Assert.That(afterSelectedStar.Values[world.Id][DailyTaskType.Comments].Progress).IsEqualTo(1L);
    }

    [Test]
    public async Task LikePost_Sunday_TracksAddedLikeOnlyInOwnedWorldAsync()
    {
        // Arrange
        Factory.Session.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero));
        using var api = Factory.CreateApiClient(8935);
        using var otherApi = Factory.CreateApiClient(8936);
        var player = await CreatePlayerWithWorldAsync(api);
        await CreatePlayerWithWorldAsync(otherApi);
        var worldId = WorldId.From(await GetWorldIdAsync(api));
        var otherWorldId = WorldId.From(await GetWorldIdAsync(otherApi));
        var own = await PublishAsync(api, worldId, "Мой новый концерт.");
        var foreign = await PublishAsync(otherApi, otherWorldId, "Выступление в другом мире.");
        var like = new LikePostRequest { AuthorId = player.Id.ToAuthorId() };

        // Act
        await api.Posts.LikePostAsync(PostId.New(Factory.Session.Clock.GetUtcNow()).ToString(), like, CancellationToken.None);
        var afterMissing = await ReadChallengeAsync(api);
        await api.Posts.LikePostAsync(foreign.Post.PostId.ToString(), like, CancellationToken.None);
        var afterForeign = await ReadChallengeAsync(api);
        var foreignChallenge = await ReadChallengeAsync(otherApi);
        await api.Posts.LikePostAsync(own.Post.PostId.ToString(), like, CancellationToken.None);
        var afterAdd = await ReadChallengeAsync(api);
        await api.Posts.LikePostAsync(own.Post.PostId.ToString(), like, CancellationToken.None);
        var afterRemove = await ReadChallengeAsync(api);

        // Assert
        await Assert.That(afterMissing.Values[worldId][DailyTaskType.Likes].Progress).IsEqualTo(0L);
        await Assert.That(afterForeign.Values[worldId][DailyTaskType.Likes].Progress).IsEqualTo(0L);
        await Assert.That(afterForeign.Values.ContainsKey(otherWorldId)).IsFalse();
        await Assert.That(foreignChallenge.Values[otherWorldId][DailyTaskType.Likes].Progress).IsEqualTo(0L);
        await Assert.That(afterAdd.Values[worldId][DailyTaskType.Likes].Progress).IsEqualTo(1L);
        await Assert.That(afterRemove.Values[worldId][DailyTaskType.Likes].Progress).IsEqualTo(1L);
    }

    private static async Task<DailyChallengeResponse> ReadChallengeAsync(TestApiClient api) =>
        (await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).DailyChallenge;

    private static Task<CreatePostResponse> PublishAsync(TestApiClient api, WorldId worldId, string text, PostId? repostId = null) =>
        api.Posts.CreatePostAsync(worldId.ToString(), new CreatePostRequest { Text = text, RepostId = repostId }, CancellationToken.None);

    private static Task<ReplyResponse> ReplyAsync(TestApiClient api, WorldId worldId, PostId postId, AuthorId authorId, CommentId? parentId = null) =>
        api.Posts.ReplyToPostAsync(
            postId.ToString(),
            worldId.ToString(),
            new ReplyRequest
            {
                AuthorId = authorId,
                CommentId = parentId,
                RequestId = Guid.NewGuid().ToString("N"),
                Text = "Обсудим это на репетиции.",
            },
            CancellationToken.None);
}
