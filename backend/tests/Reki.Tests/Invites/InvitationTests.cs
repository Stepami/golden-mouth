using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Invites;
using Reki.Domain.Player;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Persistence.Migrations;

namespace Reki.Tests.Invites;

[ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class InvitationTests(TestSessionFixture mongo)
{
    [Test]
    public async Task Create_SamePlayers_RejectsSelfInvitationAsync()
    {
        // Arrange
        var player = PlayerId.From("same-player");

        // Act
        Action create = () => Invitation.Create(player, player, DateTimeOffset.UnixEpoch);

        // Assert
        await Assert.That(create).Throws<ArgumentException>();
    }

    [Test]
    public async Task TryCreateInvite_MultipleFriends_StoresIndependentAttributionAsync()
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            await CreateIndexesAsync(root);
            var store = new MongoGameStore(root);
            var first = CreateInvitation("first-friend");
            var second = CreateInvitation("second-friend");
            var duplicate = Invitation.Create(PlayerId.From("other-inviter"), first.InviteeId, first.CreatedAtUtc);

            // Act
            var createdFirst = await store.TryCreateInviteAsync(first, CancellationToken.None);
            var createdSecond = await store.TryCreateInviteAsync(second, CancellationToken.None);
            var createdDuplicate = await store.TryCreateInviteAsync(duplicate, CancellationToken.None);
            var saved = await store.GetInviteAsync(first.InviteeId, CancellationToken.None);
            var invitations = await store.GetInvitesByInviterAsync(first.InviterId, CancellationToken.None);
            var missing = await store.GetInviteAsync(PlayerId.From("missing"), CancellationToken.None);

            // Assert
            await Assert.That(createdFirst).IsTrue();
            await Assert.That(createdSecond).IsTrue();
            await Assert.That(createdDuplicate).IsFalse();
            await Assert.That(saved).IsEqualTo(first);
            var document = await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .Find(new BsonDocument("InviteeId", first.InviteeId.Value)).SingleAsync();
            await Assert.That(document["_id"].AsString).IsEqualTo(first.Id.Value);
            await Assert.That(invitations).IsEquivalentTo(new[] { first, second });
            await Assert.That(missing).IsNull();
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("players")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("posts")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
        }
        finally
        {
            await DropAsync(root);
        }
    }

    private static Invitation CreateInvitation(string invitee) =>
        Invitation.Create(PlayerId.From("inviter"), PlayerId.From(invitee), DateTimeOffset.UnixEpoch);

    private static async Task CreateIndexesAsync(MongoCompositionRoot root)
    {
        using var session = await root.MongoClient.StartSessionAsync();
        var migration = new MoveReferralStateToInvites();
        await migration.UpAsync(root.MongoDatabase, session, CancellationToken.None);
        await migration.UpAsync(root.MongoDatabase, session, CancellationToken.None);
    }

    private static Task DropAsync(MongoCompositionRoot root) =>
        root.MongoClient.DropDatabaseAsync(root.MongoDatabase.DatabaseNamespace.DatabaseName);

    private MongoCompositionRoot CreateRoot() => new(Options.Create(new MongoOptions
    {
        ConnectionString = mongo.ConnectionString,
        DatabaseName = $"invite_tests_{Guid.NewGuid():N}",
    }));
}