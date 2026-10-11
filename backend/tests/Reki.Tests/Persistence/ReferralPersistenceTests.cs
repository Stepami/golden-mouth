using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Persistence.Migrations;
using Reki.Tests.Common;

namespace Reki.Tests.Persistence;

[ClassDataSource<StandaloneReferralMongoFixture>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Migrations", Order = 0)]
public sealed class ReferralPersistenceTests(StandaloneReferralMongoFixture mongo)
{
    [Test]
    public async Task TryCreatePlayer_StandaloneMongo_PersistsOneAttributionWithoutGrantAsync()
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            await CreateIndexesAsync(root);
            var store = new MongoGameStore(root);
            var player = TestGameData.CreatePlayer();
            var inviter = PlayerId.From("inviter");

            // Act
            var created = await store.TryCreatePlayerAsync(player, inviter, TestGameData.PeriodStart, 5, CancellationToken.None);
            var duplicate = await store.TryCreatePlayerAsync(player, inviter, TestGameData.PeriodStart, 5, CancellationToken.None);
            var state = await store.GetInviteStateAsync(player.Id, TestGameData.PeriodStart, CancellationToken.None);

            // Assert
            await Assert.That(created).IsTrue();
            await Assert.That(duplicate).IsFalse();
            await Assert.That(state).IsEqualTo(new InviteState(0, 0, 0, 5, true));
            var saved = await root.MongoDatabase.GetCollection<BsonDocument>("players")
                .Find(new BsonDocument("_id", player.Id.Value)).SingleAsync();
            await Assert.That(saved["TelegramUserId"].AsInt64).IsEqualTo(1L);
            await Assert.That(saved.Contains("ReferralBenefits")).IsFalse();
            await Assert.That(saved.Contains("InvitedBy")).IsFalse();
            var invite = await GetInviteAsync(root, player.Id);
            await Assert.That(invite["InviterId"].AsString).IsEqualTo("inviter");
            await Assert.That(invite["BonusPosts"].AsInt32).IsEqualTo(5);
            await Assert.That(invite["CreatedAtUtc"].AsBsonDateTime.ToUniversalTime()).IsEqualTo(TestGameData.PeriodStart.UtcDateTime);
            await Assert.That(invite.Contains("PendingPlayer")).IsFalse();
            await Assert.That(invite.Contains("PendingTelegramUserId")).IsFalse();
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(1L);
        }
        finally
        {
            await DropAsync(root);
        }
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task GetPlayer_InterruptedRegistration_RecoversProfileWithClearedIntentAsync(bool profileExists, bool byPlayerId)
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            await CreateIndexesAsync(root);
            var player = TestGameData.CreatePlayer();
            var intent = CreateInvite(player);
            intent["PendingPlayer"] = player.ToBsonDocument();
            intent["PendingTelegramUserId"] = player.TelegramUserId.Value;
            await root.MongoDatabase.GetCollection<BsonDocument>("invites").InsertOneAsync(intent);
            if (profileExists)
            {
                await root.MongoDatabase.GetCollection<PlayerProfile>("players").InsertOneAsync(player);
            }

            var store = new MongoGameStore(root);

            // Act
            var saved = byPlayerId
                ? await store.GetPlayerAsync(player.Id, CancellationToken.None)
                : await store.GetPlayerAsync(player.TelegramUserId, CancellationToken.None);
            var repeated = await store.GetPlayerAsync(player.Id, CancellationToken.None);

            // Assert
            await Assert.That(saved?.Id.Value).IsEqualTo("player-one");
            await Assert.That(repeated?.TelegramUserId.Value).IsEqualTo(1L);
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("players")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(1L);
            var invite = await GetInviteAsync(root, player.Id);
            await Assert.That(invite.Contains("PendingPlayer")).IsFalse();
            await Assert.That(invite.Contains("PendingTelegramUserId")).IsFalse();
            await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsFalse();
        }
        finally
        {
            await DropAsync(root);
        }
    }

    [Test]
    [Arguments("none")]
    [Arguments("Inviter")]
    [Arguments("Invitee")]
    public async Task GetInviteState_InterruptedPostAward_RepairsBothSidesExactlyOnceAsync(string completedSide)
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            await CreateIndexesAsync(root);
            var store = new MongoGameStore(root);
            var player = TestGameData.CreatePlayer();
            var inviter = PlayerId.From("inviter");
            await store.TryCreatePlayerAsync(player, inviter, TestGameData.PeriodStart, 5, CancellationToken.None);
            var empty = await store.GetInviteStateAsync(player.Id, TestGameData.PeriodStart, CancellationToken.None);
            await Assert.That(empty).IsEqualTo(new InviteState(0, 0, 0, 5, true));
            if (completedSide != "none")
            {
                await root.MongoDatabase.GetCollection<BsonDocument>("invites").UpdateOneAsync(
                    new BsonDocument("InviteeId", player.Id.Value),
                    Builders<BsonDocument>.Update.Set($"{completedSide}BonusUsed", true)
                        .Set($"{completedSide}BonusAtUtc", TestGameData.PeriodStart.AddHours(1).UtcDateTime)
                        .Set($"{completedSide}BonusPostId", "already-awarded"));
            }

            var posts = root.MongoDatabase.GetCollection<GamePost>("posts");
            await posts.InsertManyAsync([
                CreatePost(player.Id, "later", TestGameData.PeriodStart.AddHours(2)),
                CreatePost(player.Id, "first", TestGameData.PeriodStart.AddHours(1))]);

            // Act
            var states = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
                store.GetInviteStateAsync(player.Id, TestGameData.PeriodStart, CancellationToken.None).AsTask()));
            var inviterState = await store.GetInviteStateAsync(inviter, TestGameData.PeriodStart, CancellationToken.None);
            var nextDay = await store.GetInviteStateAsync(player.Id, TestGameData.PeriodStart.AddDays(1), CancellationToken.None);
            var nextInviter = await store.GetInviteStateAsync(inviter, TestGameData.PeriodStart.AddDays(1), CancellationToken.None);

            // Assert
            foreach (var state in states)
            {
                await Assert.That(state).IsEqualTo(new InviteState(0, 0, 5, 0, true));
            }

            await Assert.That(inviterState).IsEqualTo(new InviteState(1, 0, 0, 0, false));
            await Assert.That(nextDay).IsEqualTo(new InviteState(0, 0, 0, 0, true));
            await Assert.That(nextInviter).IsEqualTo(new InviteState(0, 0, 0, 0, false));
            var invite = await GetInviteAsync(root, player.Id);
            foreach (var side in new[] { "Inviter", "Invitee" })
            {
                await Assert.That(invite[$"{side}BonusUsed"].AsBoolean).IsTrue();
                await Assert.That(invite[$"{side}BonusPostId"].AsString)
                    .IsEqualTo(side == completedSide ? "already-awarded" : "first");
                await Assert.That(invite[$"{side}BonusAtUtc"].AsBsonDateTime.ToUniversalTime())
                    .IsEqualTo(TestGameData.PeriodStart.AddHours(1).UtcDateTime);
            }
        }
        finally
        {
            await DropAsync(root);
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MoveReferralState_LegacyPlayer_PreservesLegacyStateOnRerunAsync(bool used)
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            var period = TestGameData.PeriodStart;
            var player = TestGameData.CreatePlayer().ToBsonDocument();
            player["InvitedBy"] = "old-inviter";
            player["InvitedAtUtc"] = new BsonArray { period.Ticks, 0 };
            player["FirstPostInviteBonus"] = 5;
            player["FirstPostInviteBonusUsed"] = used;
            player["InviteBonusesEarnedInPeriod"] = 2;
            player["DailyBonus"] = 10;
            var players = root.MongoDatabase.GetCollection<BsonDocument>("players");
            await players.InsertOneAsync(player);
            using var session = await root.MongoClient.StartSessionAsync();
            var migration = new MoveReferralStateToInvites();

            // Act
            await migration.UpAsync(root.MongoDatabase, session, CancellationToken.None);
            await migration.UpAsync(root.MongoDatabase, session, CancellationToken.None);

            // Assert
            var invite = await GetInviteAsync(root, PlayerId.From("player-one"));
            await Assert.That(invite["InviterId"].AsString).IsEqualTo("old-inviter");
            await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsTrue();
            await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsEqualTo(used);
            await Assert.That(invite["BonusPosts"].AsInt32).IsEqualTo(5);
            await Assert.That(invite["CreatedAtUtc"].AsBsonDateTime.ToUniversalTime()).IsEqualTo(period.UtcDateTime);
            var saved = await players.Find(new BsonDocument("_id", "player-one")).SingleAsync();
            await Assert.That(saved["DailyBonus"].AsInt32).IsEqualTo(10);
            foreach (var field in new[] { "InvitedBy", "InvitedAtUtc", "FirstPostInviteBonus", "FirstPostInviteBonusUsed", "InviteBonusesEarnedInPeriod" })
            {
                await Assert.That(saved.Contains(field)).IsFalse();
            }

            var ledger = await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .Find(new BsonDocument("_id", "legacy-quota:player-one")).SingleAsync();
            await Assert.That(ledger["LegacyInviterBonuses"].AsInt32).IsEqualTo(2);
            await Assert.That(ledger.Contains("InviteeId")).IsFalse();
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(2L);
            var store = new MongoGameStore(root);
            var current = await store.GetInviteStateAsync(PlayerId.From("player-one"), period, CancellationToken.None);
            await Assert.That(current).IsEqualTo(new InviteState(0, 2, 0, used ? 0 : 5, true));
            var next = await store.GetInviteStateAsync(PlayerId.From("player-one"), period.AddDays(1), CancellationToken.None);
            await Assert.That(next.LegacyInviterBonuses).IsEqualTo(0);
            await root.MongoDatabase.GetCollection<GamePost>("posts").InsertOneAsync(
                CreatePost(PlayerId.From("player-one"), "after-migration", period.AddHours(1)));
            var afterPost = await store.GetInviteStateAsync(PlayerId.From("player-one"), period, CancellationToken.None);
            await Assert.That(afterPost.InviteePosts).IsEqualTo(used ? 0 : 5);
            await Assert.That(afterPost.PendingFirstPostBonus).IsEqualTo(0);
            var oldInviter = await store.GetInviteStateAsync(PlayerId.From("old-inviter"), period, CancellationToken.None);
            await Assert.That(oldInviter.InviterBonuses).IsEqualTo(0);
        }
        finally
        {
            await DropAsync(root);
        }
    }

    [Test]
    public async Task GetPlayer_ConflictingRegistration_DiscardsOrphanIntentWithoutChangingExistingPlayerAsync()
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            await CreateIndexesAsync(root);
            var original = TestGameData.CreatePlayer();
            var pending = original with { Id = PlayerId.From("orphan") };
            await root.MongoDatabase.GetCollection<PlayerProfile>("players").InsertOneAsync(original);
            var intent = CreateInvite(pending);
            intent["PendingPlayer"] = pending.ToBsonDocument();
            intent["PendingTelegramUserId"] = pending.TelegramUserId.Value;
            await root.MongoDatabase.GetCollection<BsonDocument>("invites").InsertOneAsync(intent);
            var store = new MongoGameStore(root);

            // Act
            var result = await store.GetPlayerAsync(original.TelegramUserId, CancellationToken.None);

            // Assert
            await Assert.That(result?.Id.Value).IsEqualTo("player-one");
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("players")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(1L);
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(0L);
        }
        finally
        {
            await DropAsync(root);
        }
    }

    [Test]
    [Arguments("date")]
    [Arguments("document")]
    [Arguments("null")]
    public async Task MoveReferralState_LegacyDateRepresentations_PreservesDateWithoutInventingQuotaAsync(string representation)
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            var period = TestGameData.PeriodStart;
            BsonValue date = representation switch
            {
                "date" => new BsonDateTime(period.UtcDateTime),
                "document" => new BsonDocument
                {
                    ["DateTime"] = new BsonDateTime(period.UtcDateTime),
                    ["Ticks"] = period.Ticks,
                    ["Offset"] = 0,
                },
                _ => BsonNull.Value,
            };
            await root.MongoDatabase.GetCollection<BsonDocument>("players").InsertManyAsync([
                new BsonDocument { ["_id"] = "friend", ["InvitedBy"] = "inviter", ["InvitedAtUtc"] = date, ["FirstPostInviteBonus"] = 5 },
                new BsonDocument { ["_id"] = "no-referral", ["DailyBonus"] = 3 },
                new BsonDocument { ["_id"] = "quota-one", ["InviteBonusesEarnedInPeriod"] = 1, ["PostsPeriodStartedAtUtc"] = date },
                new BsonDocument { ["_id"] = "quota-two", ["InviteBonusesEarnedInPeriod"] = 2, ["PostsPeriodStartedAtUtc"] = date }]);
            using var session = await root.MongoClient.StartSessionAsync();

            // Act
            await new MoveReferralStateToInvites().UpAsync(root.MongoDatabase, session, CancellationToken.None);

            // Assert
            var invite = await GetInviteAsync(root, PlayerId.From("friend"));
            await Assert.That(invite["CreatedAtUtc"]).IsEqualTo(
                representation == "null" ? BsonNull.Value : new BsonDateTime(period.UtcDateTime));
            await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsFalse();
            await Assert.That(invite["InviterBonusUsed"].AsBoolean).IsTrue();
            await Assert.That(await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).IsEqualTo(3L);
            foreach (var (id, count) in new[] { ("quota-one", 1), ("quota-two", 2) })
            {
                var quota = await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                    .Find(new BsonDocument("_id", $"legacy-quota:{id}")).SingleAsync();
                await Assert.That(quota["LegacyPeriodAtUtc"]).IsEqualTo(
                    representation == "null" ? BsonNull.Value : new BsonDateTime(period.UtcDateTime));
                await Assert.That(quota["LegacyInviterBonuses"].AsInt32).IsEqualTo(count);
                await Assert.That(quota.Contains("InviteeId")).IsFalse();
            }

            var untouched = await root.MongoDatabase.GetCollection<BsonDocument>("players")
                .Find(new BsonDocument("_id", "no-referral")).SingleAsync();
            await Assert.That(untouched["DailyBonus"].AsInt32).IsEqualTo(3);
        }
        finally
        {
            await DropAsync(root);
        }
    }

    [Test]
    public async Task MoveReferralState_InterruptedCopy_PreservesAlreadyMigratedInvitationOnResumeAsync()
    {
        // Arrange
        var root = CreateRoot();
        try
        {
            var player = TestGameData.CreatePlayer().ToBsonDocument();
            player["InvitedBy"] = "old-inviter";
            player["FirstPostInviteBonus"] = 5;
            player["FirstPostInviteBonusUsed"] = false;
            player["InviteBonusesEarnedInPeriod"] = 2;
            await root.MongoDatabase.GetCollection<BsonDocument>("players").InsertOneAsync(player);
            var copied = CreateInvite(TestGameData.CreatePlayer());
            copied["InviteeBonusUsed"] = true;
            copied["InviteeBonusPostId"] = "committed-during-recovery";
            var ledger = new BsonDocument
            {
                ["_id"] = "legacy-quota:player-one",
                ["InviterId"] = "player-one",
                ["LegacyPeriodAtUtc"] = TestGameData.PeriodStart.UtcDateTime,
                ["LegacyInviterBonuses"] = 3,
            };
            await root.MongoDatabase.GetCollection<BsonDocument>("invites").InsertManyAsync([copied, ledger]);
            using var session = await root.MongoClient.StartSessionAsync();

            // Act
            await new MoveReferralStateToInvites().UpAsync(root.MongoDatabase, session, CancellationToken.None);

            // Assert
            var invite = await GetInviteAsync(root, PlayerId.From("player-one"));
            await Assert.That(invite["InviterId"].AsString).IsEqualTo("inviter");
            await Assert.That(invite["InviteeBonusUsed"].AsBoolean).IsTrue();
            await Assert.That(invite["InviteeBonusPostId"].AsString).IsEqualTo("committed-during-recovery");
            var savedLedger = await root.MongoDatabase.GetCollection<BsonDocument>("invites")
                .Find(new BsonDocument("_id", "legacy-quota:player-one")).SingleAsync();
            await Assert.That(savedLedger["LegacyInviterBonuses"].AsInt32).IsEqualTo(3);
            var saved = await root.MongoDatabase.GetCollection<BsonDocument>("players")
                .Find(new BsonDocument("_id", "player-one")).SingleAsync();
            await Assert.That(saved.Contains("InvitedBy")).IsFalse();
            await Assert.That(saved.Contains("InviteBonusesEarnedInPeriod")).IsFalse();
        }
        finally
        {
            await DropAsync(root);
        }
    }

    private static async Task CreateIndexesAsync(MongoCompositionRoot root)
    {
        await root.MongoDatabase.GetCollection<PlayerProfile>("players").Indexes.CreateOneAsync(
            new CreateIndexModel<PlayerProfile>(
                Builders<PlayerProfile>.IndexKeys.Ascending(item => item.TelegramUserId),
                new CreateIndexOptions { Unique = true }));
        using var session = await root.MongoClient.StartSessionAsync();
        await new MoveReferralStateToInvites().UpAsync(root.MongoDatabase, session, CancellationToken.None);
    }

    private static Task DropAsync(MongoCompositionRoot root) =>
        root.MongoClient.DropDatabaseAsync(root.MongoDatabase.DatabaseNamespace.DatabaseName);

    private static Task<BsonDocument> GetInviteAsync(MongoCompositionRoot root, PlayerId player) =>
        root.MongoDatabase.GetCollection<BsonDocument>("invites").Find(new BsonDocument("InviteeId", player.Value)).SingleAsync();

    private static BsonDocument CreateInvite(PlayerProfile player) => new()
    {
        ["_id"] = player.Id.Value,
        ["InviteeId"] = player.Id.Value,
        ["InviterId"] = "inviter",
        ["CreatedAtUtc"] = TestGameData.PeriodStart.UtcDateTime,
        ["BonusPosts"] = 5,
        ["InviterBonusUsed"] = false,
        ["InviteeBonusUsed"] = false,
    };

    private static GamePost CreatePost(PlayerId player, string id, DateTimeOffset createdAt) => new(
        PostId.From(id), player, WorldId.From("world"), "пост", 1, [], [], createdAt, 0, 0, 1, false, false);

    private MongoCompositionRoot CreateRoot() => new(Options.Create(
        new MongoOptions
        {
            ConnectionString = mongo.ConnectionString,
            DatabaseName = $"referrals_{Guid.NewGuid():N}",
        }));
}
