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
    [Arguments("")]
    [Arguments("Хочу обсудить эту мысль вместе с вами.")]
    public async Task Repost_CreatesReferencedPostWithoutSkillGrowthAsync(string text)
    {
        using var api = Factory.CreateApiClient(1301);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var source = await CreateSourceAsync(worldId);
        var before = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var result = await api.Posts.CreatePostAsync(worldId, new CreatePostRequest { Text = text, RepostId = source.Id }, CancellationToken.None);
        var after = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(result.Result.Delta).IsEqualTo(1);
        await Assert.That(result.Result.Viral).IsFalse();
        await Assert.That(result.Result.Flop).IsFalse();
        await Assert.That(result.Post.RepostId).IsEqualTo(source.Id);
        await Assert.That(result.Post.Text).IsEqualTo(text);
        await Assert.That(after.Energy).IsEqualTo(before.Energy - 1);
        await Assert.That(after.Skills.All(item => item.Value == before.Skills[item.Key])).IsTrue();
        await Assert.That(after.EnergyWallet?.Crystals).IsEqualTo(before.EnergyWallet?.Crystals);
        var read = await api.Posts.GetPostAsync(result.Post.PostId.ToString(), worldId, CancellationToken.None);
        await Assert.That(read.RepostId).IsEqualTo(source.Id);
        await Database.GetCollection<GamePost>("posts").DeleteOneAsync(post => post.Id == source.Id);
        var retained = await api.Posts.GetPostAsync(result.Post.PostId.ToString(), worldId, CancellationToken.None);
        await Assert.That(retained.Text).IsEqualTo(text);
        await Assert.That(retained.RepostId).IsEqualTo(source.Id);
    }

    [Test]
    public async Task Repost_AnotherWorld_DoesNotSpendEnergyAsync()
    {
        using var api = Factory.CreateApiClient(1302);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var source = await CreateSourceAsync(worldId);
        var second = await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var before = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        var missing = await Assert.ThrowsAsync<PostsApiException>(() => api.Posts.CreatePostAsync(second.Id.ToString(), new CreatePostRequest { Text = string.Empty, RepostId = source.Id }, CancellationToken.None));
        await Assert.That(missing?.StatusCode).IsEqualTo((int)HttpStatusCode.NotFound);
        var after = await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await Assert.That(after.Energy).IsEqualTo(before.Energy);
    }

    [Test]
    public async Task Repost_OriginalCommenter_IsReusedAsync()
    {
        using var api = Factory.CreateApiClient(1303);
        await CreatePlayerWithWorldAsync(api);
        var worldId = await GetWorldIdAsync(api);
        var source = await CreateSourceAsync(worldId);
        var world = await Database.GetCollection<World>("worlds").Find(item => item.Id == source.WorldId).SingleAsync();
        var author = world.Stars[1].Id.ToAuthorId();
        var comment = new GameComment(CommentId.New(), null, null, author, "Обсудим эту идею.", false, [], null);
        await Database.GetCollection<GamePost>("posts").UpdateOneAsync(post => post.Id == source.Id, Builders<GamePost>.Update.Set(post => post.Comments, new[] { comment }));
        var result = await api.Posts.CreatePostAsync(worldId, new CreatePostRequest { Text = string.Empty, RepostId = source.Id }, CancellationToken.None);
        await Assert.That(result.PredictedAuthorId).IsEqualTo(author);
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
