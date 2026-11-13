using System.Net;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class PostLikesTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Like_OwnPost_TogglesWithoutChangingAudienceLikesOrPlayerAsync()
    {
        const long user = 8201;
        var (postId, _) = await PublishPostForThreadAsync(user);
        using var api = Factory.CreateApiClient(user);
        var worldId = await GetWorldIdAsync(api);
        var player = await GetStoredPlayerAsync(user);
        await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
            new BsonDocument("_id", player["_id"]), Builders<BsonDocument>.Update.Set("Wallet.Energy", 0));
        var beforePlayer = await GetStoredPlayerAsync(user);
        var authorId = $"Player_{beforePlayer["_id"].AsBsonBinaryData.ToGuid():N}";
        var posts = Database.GetCollection<BsonDocument>("posts");
        var filter = new BsonDocument("_id", new BsonBinaryData(Guid.Parse(postId), GuidRepresentation.Standard));
        await posts.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("Likes", 20L).Set("LikedBy", new BsonArray()));
        var beforePost = await GetStoredPostAsync(postId);
        var beforeWorld = await GetStoredWorldAsync(user);
        var requests = Factory.Llm.RequestCount;
        using var http = Factory.CreateClient();
        http.SetFakeJwtBearerToken(new Dictionary<string, object> { ["sub"] = beforePlayer["_id"].AsBsonBinaryData.ToGuid().ToString("N") });

        using var first = await ToggleAsync(http, postId, worldId, authorId);
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var firstBody = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        await Assert.That(firstBody.RootElement.GetProperty("likes").GetInt64()).IsEqualTo(20);
        await Assert.That(firstBody.RootElement.GetProperty("likedBy").EnumerateArray().Single().GetString()).IsEqualTo(authorId);

        using var second = await ToggleAsync(http, postId, worldId, authorId);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var secondBody = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        await Assert.That(secondBody.RootElement.GetProperty("likes").GetInt64()).IsEqualTo(20);
        await Assert.That(secondBody.RootElement.GetProperty("likedBy").GetArrayLength()).IsEqualTo(0);
        var afterPost = await GetStoredPostAsync(postId);
        await Assert.That(afterPost["Comments"]).IsEqualTo(beforePost["Comments"]);
        await Assert.That(afterPost["Views"]).IsEqualTo(beforePost["Views"]);
        await Assert.That(afterPost["Likes"].ToInt64()).IsEqualTo(20);
        await Assert.That((await GetStoredPlayerAsync(user))["Wallet"]).IsEqualTo(beforePlayer["Wallet"]);
        await Assert.That(await GetStoredWorldAsync(user)).IsEqualTo(beforeWorld);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(requests);
    }

    [Test]
    public async Task Liked_FilterIsAppliedBeforePagination_UnlikeRemovesMatchingPostAsync()
    {
        const long user = 8202;
        var (postId, _) = await PublishPostForThreadAsync(user);
        using var api = Factory.CreateApiClient(user);
        var worldId = await GetWorldIdAsync(api);
        var player = await GetStoredPlayerAsync(user);
        var playerGuid = player["_id"].AsBsonBinaryData.ToGuid();
        var authorId = $"Player_{playerGuid:N}";
        var posts = Database.GetCollection<BsonDocument>("posts");
        var source = await GetStoredPostAsync(postId);
        var start = Factory.Session.Clock.GetUtcNow().AddMinutes(-10);
        var ids = Enumerable.Range(0, 5).Select(index => PostId.New(start.AddMinutes(index))).ToArray();
        var records = ids.Select((id, index) =>
        {
            var post = source.DeepClone().AsBsonDocument;
            post["_id"] = new BsonBinaryData(id.Value, GuidRepresentation.Standard);
            post["AuthorId"] = index == 2 ? "Star_other" : authorId;
            post["Text"] = $"liked-page-{index}";
            post["Likes"] = 4L;
            post["LikedBy"] = index % 2 == 0 ? new BsonArray { authorId } : new BsonArray();
            return post;
        }).ToArray();
        await posts.InsertManyAsync(records);
        using var http = Factory.CreateClient();
        http.SetFakeJwtBearerToken(new Dictionary<string, object> { ["sub"] = playerGuid.ToString("N") });

        using var pageOne = await http.GetAsync($"/posts/liked?worldId={worldId}&likedBy={authorId}&pageSize=2");
        await Assert.That(pageOne.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var firstBody = JsonDocument.Parse(await pageOne.Content.ReadAsStringAsync());
        var firstItems = firstBody.RootElement.GetProperty("items");
        await Assert.That(firstItems.GetArrayLength()).IsEqualTo(2);
        await Assert.That(firstItems[0].GetProperty("text").GetString()).IsEqualTo("liked-page-4");
        await Assert.That(firstItems[1].GetProperty("text").GetString()).IsEqualTo("liked-page-2");
        var cursor = firstBody.RootElement.GetProperty("nextCursor").GetString();
        await Assert.That(cursor).IsNotNull();
        using var pageTwo = await http.GetAsync($"/posts/liked?worldId={worldId}&likedBy={authorId}&pageSize=2&cursor={cursor}");
        await Assert.That(pageTwo.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var secondBody = JsonDocument.Parse(await pageTwo.Content.ReadAsStringAsync());
        var secondItems = secondBody.RootElement.GetProperty("items");
        await Assert.That(secondItems.GetArrayLength()).IsEqualTo(1);
        await Assert.That(secondItems[0].GetProperty("text").GetString()).IsEqualTo("liked-page-0");
        await Assert.That(secondBody.RootElement.GetProperty("nextCursor").ValueKind).IsEqualTo(JsonValueKind.Null);

        using var toggle = await ToggleAsync(http, ids[2].ToString(), worldId, authorId);
        await Assert.That(toggle.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using var refreshed = await http.GetAsync($"/posts/liked?worldId={worldId}&likedBy={authorId}&pageSize=10");
        using var refreshedBody = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        await Assert.That(refreshedBody.RootElement.GetProperty("items").EnumerateArray()
            .Any(item => item.GetProperty("text").GetString() == "liked-page-2")).IsFalse();
        var saved = await GetStoredPostAsync(ids[2].ToString());
        await Assert.That(saved["Likes"].ToInt64()).IsEqualTo(4);
        await Assert.That(saved["LikedBy"].AsBsonArray).IsEmpty();
    }

    [Test]
    public async Task Like_ConcurrentAuthors_DoNotLoseLikesOrCommentsAsync()
    {
        const long user = 8204;
        var (postId, _) = await PublishPostForThreadAsync(user);
        using var api = Factory.CreateApiClient(user);
        var worldId = await GetWorldIdAsync(api);
        var player = await GetStoredPlayerAsync(user);
        var playerGuid = player["_id"].AsBsonBinaryData.ToGuid();
        var playerAuthor = $"Player_{playerGuid:N}";
        const string starAuthor = "Star_future";
        var before = await GetStoredPostAsync(postId);
        using var http = Factory.CreateClient();
        http.SetFakeJwtBearerToken(new Dictionary<string, object> { ["sub"] = playerGuid.ToString("N") });

        var responses = await Task.WhenAll(ToggleAsync(http, postId, worldId, playerAuthor), ToggleAsync(http, postId, worldId, starAuthor));
        foreach (var response in responses)
        {
            using (response)
            {
                await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
            }
        }

        var saved = await GetStoredPostAsync(postId);
        await Assert.That(saved["LikedBy"].AsBsonArray.Select(value => value.AsString).ToArray())
            .IsEquivalentTo([playerAuthor, starAuthor]);
        await Assert.That(saved["Comments"]).IsEqualTo(before["Comments"]);
        await Assert.That(saved["Likes"]).IsEqualTo(before["Likes"]);
    }

    [Test]
    public async Task Likes_RejectsInvalidRequestsAsync()
    {
        const long user = 8203;
        var (postId, _) = await PublishPostForThreadAsync(user);
        using var api = Factory.CreateApiClient(user);
        var worldId = await GetWorldIdAsync(api);
        var player = await GetStoredPlayerAsync(user);
        var playerGuid = player["_id"].AsBsonBinaryData.ToGuid();
        var authorId = $"Player_{playerGuid:N}";
        using var http = Factory.CreateClient();
        http.SetFakeJwtBearerToken(new Dictionary<string, object> { ["sub"] = playerGuid.ToString("N") });
        var before = await GetStoredPostAsync(postId);

        using var missingAuthor = await http.GetAsync($"/posts/liked?worldId={worldId}");
        using var invalidAuthor = await http.GetAsync($"/posts/liked?worldId={worldId}&likedBy=invalid");
        await Assert.That(missingAuthor.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(invalidAuthor.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var secondWorld = await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        using var wrongWorld = await ToggleAsync(http, postId, secondWorld.Id.ToString(), authorId);
        await Assert.That(wrongWorld.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(await GetStoredPostAsync(postId)).IsEqualTo(before);
    }

    private static Task<HttpResponseMessage> ToggleAsync(HttpClient client, string postId, string worldId, string authorId)
    {
        var content = new StringContent($"{{\"authorId\":\"{authorId}\"}}", Encoding.UTF8, "application/json");
        return client.PostAsync($"/posts/{postId}/like?worldId={worldId}", content);
    }
}
