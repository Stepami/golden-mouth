using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Social;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class SocialControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task SocialReads_ReturnEmptyCompatibilityResponsesAsync()
    {
        using var api = Factory.CreateApiClient(8392);

        var friends = await api.Social.GetFriendsAsync(CancellationToken.None);
        var challenge = await api.Social.GetActiveChallengeAsync(CancellationToken.None);
        var dm = await api.Social.GetDirectMessagesAsync("sonya", "unused-world", CancellationToken.None);

        await Assert.That(friends.Count).IsEqualTo(0);
        await Assert.That(challenge).IsNull();
        await Assert.That(dm.Messages.Count).IsEqualTo(0);
        await Assert.That(dm.Memory.Count).IsEqualTo(0);
        await Assert.That(dm.Ideas.Count).IsEqualTo(0);
        await Assert.That(dm.Star.Id).IsEqualTo(string.Empty);
        await Assert.That(dm.Status).IsEqualTo(string.Empty);
        await Assert.That(dm.RepliesLeft).IsEqualTo(0);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(0);
    }

    [Test]
    public async Task SocialWrites_ReturnEmptyWithoutPersistingOrCallingAiAsync()
    {
        using var api = Factory.CreateApiClient(8393);

        var sent = await api.Social.SendDirectMessageAsync(
            "sonya", "unused-world", new DirectMessageRequest { Text = "Привет" }, CancellationToken.None);
        await api.Social.ForgetStarMemoryAsync(
            "sonya", "unused-world", new ForgetMemoryRequest { Fact = "Факт" }, CancellationToken.None);
        await api.Social.StartChallengeAsync(new StartChallengeRequest { FriendId = "unused-friend" }, CancellationToken.None);

        await Assert.That(sent.Reply).IsNull();
        await Assert.That(sent.RepliesLeft).IsEqualTo(0);
        await Assert.That(Factory.Llm.RequestCount).IsEqualTo(0);
        foreach (var collection in new[] { "players", "stars_memory", "challenges" })
        {
            await Assert.That(await Database.GetCollection<BsonDocument>(collection)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: CancellationToken.None)).IsEqualTo(0);
        }
    }
}
