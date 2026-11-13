using System.Net;
using MongoDB.Driver;
using Reki.Api.Requests.Posts;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class RepostsControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Repost_ConcurrentRetryUndoRestore_CommitsOnceAsync()
    {
        using var api = Factory.CreateApiClient(1201);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var source = await CreateSourceAsync(worldId);
        var before = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            api.Posts.RepostPostAsync(source.Id.ToString(), worldId, CancellationToken.None)));
        var after = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var delta = results.Sum(result => result.SubscriberDelta);
        await Assert.That(delta).IsGreaterThan(0);
        await Assert.That(results.Select(result => result.PostId).Distinct().Count()).IsEqualTo(1);
        await Assert.That(after.Energy).IsEqualTo(before.Energy - 1);
        await Assert.That(after.Skills.All(item => item.Value == before.Skills[item.Key])).IsTrue();
        await Assert.That(after.EnergyWallet?.Crystals).IsEqualTo(before.EnergyWallet?.Crystals);
        var channel = await api.Posts.GetChannelPostsAsync(worldId, null, null, CancellationToken.None);
        await Assert.That(channel.Items.Count).IsEqualTo(1);
        await Assert.That(channel.Items[0].Quote?.Text).IsEqualTo(source.Text);
        await api.Posts.UndoRepostAsync(source.Id.ToString(), worldId, CancellationToken.None);
        var hidden = await api.Posts.GetChannelPostsAsync(worldId, null, null, CancellationToken.None);
        await Assert.That(hidden.Items.Count).IsEqualTo(0);
        var restored = await api.Posts.RepostPostAsync(source.Id.ToString(), worldId, CancellationToken.None);
        await Assert.That(restored.SubscriberDelta).IsEqualTo(0);
        await Assert.That(restored.EnergyLeft).IsEqualTo(after.Energy);
        await Assert.That(restored.Subscribers).IsEqualTo(results[0].Subscribers);
    }

    [Test]
    public async Task Repost_InsufficientEnergyOrAnotherWorld_DoesNotCreateOrRewardAsync()
    {
        using var api = Factory.CreateApiClient(1202);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var source = await CreateSourceAsync(worldId);
        var before = await GetStoredPlayerAsync(1202);
        await Database.GetCollection<MongoDB.Bson.BsonDocument>("players").UpdateOneAsync(
            new MongoDB.Bson.BsonDocument("_id", before["_id"]),
            MongoDB.Bson.BsonDocument.Parse("{ '$set': { 'Wallet.Energy': 0 } }"));
        var conflict = await Assert.ThrowsAsync<PostsApiException>(() =>
            api.Posts.RepostPostAsync(source.Id.ToString(), worldId, CancellationToken.None));
        await Assert.That(conflict?.StatusCode).IsEqualTo((int)HttpStatusCode.Conflict);
        var second = await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var missing = await Assert.ThrowsAsync<PostsApiException>(() =>
            api.Posts.RepostPostAsync(source.Id.ToString(), second.Id.ToString(), CancellationToken.None));
        await Assert.That(missing?.StatusCode).IsEqualTo((int)HttpStatusCode.NotFound);
        var channel = await api.Posts.GetChannelPostsAsync(worldId, null, null, CancellationToken.None);
        await Assert.That(channel.Items.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Quote_StoredReference_DeletedSourceIsRedactedAsync()
    {
        using var api = Factory.CreateApiClient(1203);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var source = await CreateSourceAsync(worldId);
        const string ownText = "Мне нравится эта идея: давайте обсудим её вместе.";
        var before = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var quote = await api.Posts.CreatePostAsync(worldId, new CreatePostRequest { Text = ownText, QuotedPostId = source.Id }, CancellationToken.None);
        var after = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(after.Energy).IsEqualTo(before.Energy - 1);
        await Assert.That(quote.Post.Text).IsEqualTo(ownText);
        await Assert.That(quote.Post.Quote?.Text).IsEqualTo(source.Text);
        await Database.GetCollection<GamePost>("posts").DeleteOneAsync(post => post.Id == source.Id);
        var read = await api.Posts.GetPostAsync(quote.Post.PostId.ToString(), worldId, CancellationToken.None);
        await Assert.That(read.Text).IsEqualTo(ownText);
        await Assert.That(read.Quote?.Text).IsNull();
        await Assert.That(read.Quote?.Author).IsNull();
    }

    private async Task<GamePost> CreateSourceAsync(string worldId)
    {
        var id = WorldId.From(worldId);
        var world = await Database.GetCollection<World>("worlds").Find(item => item.Id == id).SingleAsync();
        var now = Factory.Session.Clock.GetUtcNow();
        var post = new GamePost(PostId.New(now), world.Stars[0].Id.ToAuthorId(), world.Id, "Сегодня обсуждаем новую идею для нашего мира.", 50, [], now, 0, 0.5, 1, false, false);
        await Database.GetCollection<GamePost>("posts").InsertOneAsync(post);
        return post;
    }
}
