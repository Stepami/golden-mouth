using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Api.Requests.Player;
using Reki.Domain.Player;

namespace Reki.Tests.Api;

[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
[ParallelGroup("Api", Order = 1)]
[NotInParallel("ApiState")]
public sealed class EnergyWalletControllerTests(ApiFactory factory) : GameApiTestBase(factory)
{
    [Test]
    public async Task Onboarding_NewWorlds_KeepAccountWalletAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28401);
        var onboarded = await CreatePlayerWithWorldAsync(api);

        // Act
        var initial = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        await SetWalletAsync(28401, 4, 7);
        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var after = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        var worlds = await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None);

        // Assert
        await Assert.That(onboarded.Energy).IsEqualTo(10);
        await Assert.That(initial.Amount).IsEqualTo(10);
        await Assert.That(initial.Crystals).IsEqualTo(10);
        await Assert.That(initial.Cap).IsEqualTo(10);
        await Assert.That(initial.RegenSeconds).IsEqualTo(600);
        await Assert.That(initial.NextEnergyAtUtc).IsNull();
        await Assert.That(after.Amount).IsEqualTo(4);
        await Assert.That(after.Crystals).IsEqualTo(7);
        await Assert.That(worlds.Count).IsEqualTo(2);
        await Assert.That(worlds.SelectMany(world => world.Stars).All(star => star.ChannelSubscribers is null)).IsTrue();
    }

    [Test]
    [Arguments(0, 0)]
    [Arguments(6, 6)]
    [Arguments(21, 10)]
    public async Task Wallet_LegacyProfile_InitializesOnceWithoutRetroactiveRegenAsync(int oldEnergy, int expectedEnergy)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28402);
        await CreatePlayerWithWorldAsync(api);
        await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
            new BsonDocument("TelegramUserId", 28402L),
            new BsonDocument
            {
                ["$set"] = new BsonDocument { ["Energy"] = oldEnergy, ["EnergyUsed"] = 7 },
                ["$unset"] = new BsonDocument { ["EnergyUpdatedAtUtc"] = 1, ["Crystals"] = 1, ["EnergyExchangeRequestIds"] = 1 },
            });

        // Act
        var first = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        var second = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        var stored = await GetStoredPlayerAsync(28402);

        // Assert
        await Assert.That(first.Amount).IsEqualTo(expectedEnergy);
        await Assert.That(first.Crystals).IsEqualTo(10);
        await Assert.That(second.StateVersion).IsEqualTo(first.StateVersion);
        await Assert.That(second.Crystals).IsEqualTo(10);
        await Assert.That(first.NextEnergyAtUtc).IsEqualTo(expectedEnergy < 10 ? Factory.Session.Clock.GetUtcNow().AddSeconds(600) : null);
        await Assert.That(stored["EnergyUsed"].AsInt32).IsEqualTo(7);
    }

    [Test]
    [Arguments(599, 7, 600)]
    [Arguments(600, 8, 1200)]
    [Arguments(1259, 9, 1800)]
    [Arguments(86400, 10, 0)]
    public async Task Wallet_ServerTime_RegeneratesWholeTicksAsync(int seconds, int amount, int nextSeconds)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28403);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28403, 7, 10);
        var start = Factory.Session.Clock.GetUtcNow();
        Factory.Session.Clock.Advance(TimeSpan.FromSeconds(seconds));

        // Act
        var wallet = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        var stored = await GetStoredPlayerAsync(28403);

        // Assert
        await Assert.That(wallet.Amount).IsEqualTo(amount);
        await Assert.That(wallet.NextEnergyAtUtc).IsEqualTo(nextSeconds == 0 ? null : start.AddSeconds(nextSeconds));
        await Assert.That(stored["EnergyUsed"].AsInt32).IsEqualTo(7);
    }

    [Test]
    public async Task DailyReset_PartialWallet_DoesNotRefillOrResetRegenClockAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28404);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28404, 4, 7);
        await Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            player => player.TelegramUserId == TelegramUserId.From(28404),
            Builders<PlayerProfile>.Update.Set(player => player.PostsPeriodStartedAtUtc, Factory.Session.Clock.GetUtcNow().AddDays(-2)));
        var before = await api.Player.GetEnergyWalletAsync(CancellationToken.None);

        // Act
        await ResetGameDayAsync();
        var after = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        var stored = await GetStoredPlayerAsync(28404);

        // Assert
        await Assert.That(after.Amount).IsEqualTo(4);
        await Assert.That(after.Crystals).IsEqualTo(7);
        await Assert.That(after.NextEnergyAtUtc).IsEqualTo(before.NextEnergyAtUtc);
        await Assert.That(stored["EnergyUsed"].AsInt32).IsEqualTo(0);
    }

    [Test]
    [Arguments(3, 0)]
    [Arguments(10, 7)]
    public async Task Exchange_PartialEnergy_RefillsIdempotentlyToTenAsync(int crystals, int remainingCrystals)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28405);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28405, 9, crystals);
        var before = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        var request = new ExchangeEnergyRequest("exchange-one", before.StateVersion);

        // Act
        var exchanged = await api.Player.ExchangeEnergyAsync(request, CancellationToken.None);
        await SetWalletAsync(28405, 8, exchanged.Crystals);
        var retry = await api.Player.ExchangeEnergyAsync(request, CancellationToken.None);
        var stored = await GetStoredPlayerAsync(28405);

        // Assert
        await Assert.That(before.ExchangeQuote.CrystalCost).IsEqualTo(3);
        await Assert.That(before.ExchangeQuote.TargetEnergy).IsEqualTo(10);
        await Assert.That(before.ExchangeQuote.Available).IsTrue();
        await Assert.That(exchanged.Amount).IsEqualTo(10);
        await Assert.That(exchanged.Crystals).IsEqualTo(remainingCrystals);
        await Assert.That(exchanged.NextEnergyAtUtc).IsNull();
        await Assert.That(retry.Amount).IsEqualTo(8);
        await Assert.That(retry.Crystals).IsEqualTo(remainingCrystals);
        await Assert.That(stored["EnergyUsed"].AsInt32).IsEqualTo(7);
        await Assert.That(stored["EnergyExchangeRequestIds"].AsBsonArray.Count).IsEqualTo(1);
    }

    [Test]
    [Arguments(10, 10, "energy_full")]
    [Arguments(9, 2, "insufficient_crystals")]
    public async Task Exchange_Unavailable_RejectsWithoutDebitAsync(int energy, int crystals, string reason)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28406);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28406, energy, crystals);
        var before = await api.Player.GetEnergyWalletAsync(CancellationToken.None);

        // Act
        var failure = await Assert.ThrowsAsync<PlayerApiException>(() => api.Player.ExchangeEnergyAsync(
            new ExchangeEnergyRequest("unavailable", before.StateVersion), CancellationToken.None));
        var after = await api.Player.GetEnergyWalletAsync(CancellationToken.None);

        // Assert
        await Assert.That(failure).IsNotNull();
        await Assert.That(failure.StatusCode).IsEqualTo(409);
        await Assert.That(before.ExchangeQuote.Available).IsFalse();
        await Assert.That(before.ExchangeQuote.UnavailableReason).IsEqualTo(reason);
        await Assert.That(after).IsEqualTo(before);
    }

    [Test]
    [Arguments(true, 2)]
    [Arguments(false, 1)]
    public async Task Exchange_ConcurrentRequests_CommitOneDebitAsync(bool sameRequestId, int successes)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28407);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28407, 1, 10);
        var wallet = await api.Player.GetEnergyWalletAsync(CancellationToken.None);

        // Act
        var results = await Task.WhenAll(
            ExchangeStatusAsync(api, new ExchangeEnergyRequest("race-one", wallet.StateVersion)),
            ExchangeStatusAsync(api, new ExchangeEnergyRequest(sameRequestId ? "race-one" : "race-two", wallet.StateVersion)));
        var stored = await GetStoredPlayerAsync(28407);

        // Assert
        await Assert.That(results.Count(status => status == 200)).IsEqualTo(successes);
        await Assert.That(results.All(status => status is 200 or 409)).IsTrue();
        await Assert.That(stored["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(stored["Crystals"].AsInt32).IsEqualTo(7);
        await Assert.That(stored["EnergyExchangeRequestIds"].AsBsonArray.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Exchange_ConcurrentSkillUpdate_PreservesProgressAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28408);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28408, 5, 10);
        var wallet = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
            new BsonDocument("TelegramUserId", 28408L),
            new BsonDocument { ["$set"] = new BsonDocument("Skills.brave.Experience", 42), ["$inc"] = new BsonDocument("Version", 1) });
        var progress = (await GetStoredPlayerAsync(28408))["Skills"].DeepClone();

        // Act
        var status = await ExchangeStatusAsync(api, new ExchangeEnergyRequest("stale", wallet.StateVersion));
        var fresh = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        await api.Player.ExchangeEnergyAsync(new ExchangeEnergyRequest("fresh", fresh.StateVersion), CancellationToken.None);
        var stored = await GetStoredPlayerAsync(28408);

        // Assert
        await Assert.That(status).IsEqualTo(409);
        await Assert.That(stored["Skills"]).IsEqualTo(progress);
        await Assert.That(stored["Crystals"].AsInt32).IsEqualTo(7);
        await Assert.That((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Count).IsEqualTo(1);
    }

    [Test]
    public async Task Publish_AfterFullIdle_StartsFreshRegenIntervalAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28409);
        await CreatePlayerWithWorldAsync(api);
        Factory.Session.Clock.Advance(TimeSpan.FromDays(1));
        var spentAt = Factory.Session.Clock.GetUtcNow();

        // Act
        await PublishAsync(28409, "пост после полного запаса");
        var afterPost = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        Factory.Session.Clock.Advance(TimeSpan.FromSeconds(599));
        var beforeTick = await api.Player.GetEnergyWalletAsync(CancellationToken.None);
        Factory.Session.Clock.Advance(TimeSpan.FromSeconds(1));
        var afterTick = await api.Player.GetEnergyWalletAsync(CancellationToken.None);

        // Assert
        await Assert.That(afterPost.Amount).IsEqualTo(9);
        await Assert.That(afterPost.NextEnergyAtUtc).IsEqualTo(spentAt.AddSeconds(600));
        await Assert.That(beforeTick.Amount).IsEqualTo(9);
        await Assert.That(afterTick.Amount).IsEqualTo(10);
        await Assert.That(afterTick.NextEnergyAtUtc).IsNull();
        await Assert.That((await GetStoredPlayerAsync(28409))["EnergyUsed"].AsInt32).IsEqualTo(1);
    }

    private static async Task<int> ExchangeStatusAsync(TestApiClient api, ExchangeEnergyRequest request)
    {
        try
        {
            await api.Player.ExchangeEnergyAsync(request, CancellationToken.None);
            return 200;
        }
        catch (PlayerApiException exception)
        {
            return exception.StatusCode;
        }
    }

    private Task SetWalletAsync(long userId, int energy, int crystals) =>
        Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            player => player.TelegramUserId == TelegramUserId.From(userId),
            Builders<PlayerProfile>.Update
                .Set(player => player.Energy, energy)
                .Set(player => player.Crystals, crystals)
                .Set(player => player.EnergyUsed, 7)
                .Set(player => player.EnergyUpdatedAtUtc, Factory.Session.Clock.GetUtcNow())
                .Inc(player => player.Version, 1));
}
