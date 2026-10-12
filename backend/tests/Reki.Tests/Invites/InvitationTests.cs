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
    [Arguments(false)]
    [Arguments(true)]
    public async Task UseBonus_UnusedSide_ChangesOnlyThatSideAsync(bool inviter)
    {
        // Arrange
        var original = CreateInvitation("friend");
        var now = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(3));

        // Act
        var used = inviter ? original.UseInviterBonus(now) : original.UseInviteeBonus(now);
        var repeated = inviter ? used.UseInviterBonus(now.AddDays(1)) : used.UseInviteeBonus(now.AddDays(1));

        // Assert
        await Assert.That(original.InviterBonusUsed).IsFalse();
        await Assert.That(original.InviteeBonusUsed).IsFalse();
        await Assert.That(used.InviterBonusUsed).IsEqualTo(inviter);
        await Assert.That(used.InviteeBonusUsed).IsEqualTo(!inviter);
        await Assert.That(original.InviterBonusUsedAtUtc).IsNull();
        await Assert.That(original.InviteeBonusUsedAtUtc).IsNull();
        await Assert.That(used.InviterBonusUsedAtUtc).IsEqualTo(inviter ? now.ToUniversalTime() : (DateTimeOffset?)null);
        await Assert.That(used.InviteeBonusUsedAtUtc).IsEqualTo(inviter ? (DateTimeOffset?)null : now.ToUniversalTime());
        await Assert.That((inviter ? used.InviterBonusUsedAtUtc : used.InviteeBonusUsedAtUtc)?.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(ReferenceEquals(used, repeated)).IsTrue();
        await Assert.That(used.Id).IsEqualTo(original.Id);
        await Assert.That(used.InviterId).IsEqualTo(original.InviterId);
        await Assert.That(used.InviteeId).IsEqualTo(original.InviteeId);
        await Assert.That(used.CreatedAtUtc).IsEqualTo(original.CreatedAtUtc);
    }

    [Test]
    [Arguments(-3)]
    [Arguments(3)]
    public async Task Create_NonUtcTimestamp_NormalizesCreationTimeAsync(int offsetHours)
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(offsetHours));

        // Act
        var invitation = Invitation.Create(PlayerId.From("inviter"), PlayerId.From("friend"), createdAt);

        // Assert
        await Assert.That(invitation.CreatedAtUtc).IsEqualTo(new DateTimeOffset(2026, 10, 5, 12 - offsetHours, 0, 0, TimeSpan.Zero));
        await Assert.That(invitation.CreatedAtUtc.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(invitation.InviterBonusUsed).IsFalse();
        await Assert.That(invitation.InviteeBonusUsed).IsFalse();
        await Assert.That(invitation.InviterBonusUsedAtUtc).IsNull();
        await Assert.That(invitation.InviteeBonusUsedAtUtc).IsNull();
    }

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

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task TrySaveBonus_IndependentSides_PreservesBothFlagsAsync(bool inviterFirst)
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            await CreateIndexesAsync(root);
            var store = new MongoGameStore(root);
            var original = CreateInvitation("friend");
            await store.TryCreateInviteAsync(original, CancellationToken.None);
            var inviterAt = DateTimeOffset.UnixEpoch.AddHours(1);
            var inviteeAt = DateTimeOffset.UnixEpoch.AddHours(2);

            // Act
            var first = inviterFirst
                ? await store.TrySaveInviterBonusAsync(original.UseInviterBonus(inviterAt), CancellationToken.None)
                : await store.TrySaveInviteeBonusAsync(original.UseInviteeBonus(inviteeAt), CancellationToken.None);
            var second = inviterFirst
                ? await store.TrySaveInviteeBonusAsync(original.UseInviteeBonus(inviteeAt), CancellationToken.None)
                : await store.TrySaveInviterBonusAsync(original.UseInviterBonus(inviterAt), CancellationToken.None);
            var repeated = await store.TrySaveInviterBonusAsync(original.UseInviterBonus(inviterAt.AddDays(1)), CancellationToken.None);
            var repeatInvitee = await store.TrySaveInviteeBonusAsync(
                original.UseInviteeBonus(inviteeAt.AddDays(1)), CancellationToken.None);
            var saved = await store.GetInviteAsync(original.InviteeId, CancellationToken.None);

            // Assert
            await Assert.That(first).IsTrue();
            await Assert.That(second).IsTrue();
            await Assert.That(repeated).IsFalse();
            await Assert.That(repeatInvitee).IsFalse();
            await Assert.That(saved).IsEqualTo(original.UseInviterBonus(inviterAt).UseInviteeBonus(inviteeAt));
            await Assert.That(saved?.InviterBonusUsedAtUtc).IsEqualTo(inviterAt);
            await Assert.That(saved?.InviteeBonusUsedAtUtc).IsEqualTo(inviteeAt);
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