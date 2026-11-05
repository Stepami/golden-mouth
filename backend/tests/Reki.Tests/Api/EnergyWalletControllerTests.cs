using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Player.Responses;
using Reki.Domain.Player;
using Reki.Infrastructure.Jobs;

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
        var initial = await ReadWalletAsync(api);
        await SetWalletAsync(28401, 4, 7);
        await api.Worlds.CreateWorldAsync(await CreateWorldDefinitionAsync(api), CancellationToken.None);
        var after = await ReadWalletAsync(api);
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
    public async Task Wallet_StaleEnergy_ReadsWithoutAnyWriteAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28402);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28402, 0, 7);
        var before = await GetStoredPlayerAsync(28402);
        Factory.Session.Clock.Advance(TimeSpan.FromDays(1));

        // Act
        var first = await ReadWalletAsync(api);
        await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None);
        var after = await GetStoredPlayerAsync(28402);

        // Assert
        await Assert.That(first.Amount).IsEqualTo(0);
        await Assert.That(first.Crystals).IsEqualTo(7);
        await Assert.That(after).IsEqualTo(before);
        await Assert.That(after.Contains("Energy")).IsFalse();
        await Assert.That(after.Contains("Crystals")).IsFalse();
    }

    [Test]
    [Arguments(599, 7, 600)]
    [Arguments(600, 8, 1200)]
    [Arguments(1259, 9, 1800)]
    [Arguments(86400, 10, 0)]
    public async Task Recovery_ElapsedIntervals_CatchesUpWithoutChangingOtherStateAsync(int seconds, int amount, int nextSeconds)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28403);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28403, 7, 10);
        var before = await GetStoredPlayerAsync(28403);
        var start = Factory.Session.Clock.GetUtcNow();
        Factory.Session.Clock.Advance(TimeSpan.FromSeconds(seconds));

        // Act
        await RestoreEnergyAsync();
        var wallet = await ReadWalletAsync(api);
        var after = await GetStoredPlayerAsync(28403);
        await RestoreEnergyAsync();
        var repeated = await GetStoredPlayerAsync(28403);

        // Assert
        await Assert.That(wallet.Amount).IsEqualTo(amount);
        await Assert.That(wallet.NextEnergyAtUtc).IsEqualTo(nextSeconds == 0 ? null : start.AddSeconds(nextSeconds));
        await Assert.That(after["Wallet"]["EnergyUsed"].AsInt32).IsEqualTo(7);
        await Assert.That(after["Wallet"]["Crystals"]).IsEqualTo(before["Wallet"]["Crystals"]);
        await Assert.That(after["Skills"]).IsEqualTo(before["Skills"]);
        await Assert.That(after["Settings"]).IsEqualTo(before["Settings"]);
        await Assert.That(after["PostsPeriodStartedAtUtc"]).IsEqualTo(before["PostsPeriodStartedAtUtc"]);
        await Assert.That(repeated).IsEqualTo(after);
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
        var before = await ReadWalletAsync(api);

        // Act
        await ResetGameDayAsync();
        var after = await ReadWalletAsync(api);
        var stored = await GetStoredPlayerAsync(28404);

        // Assert
        await Assert.That(after).IsEqualTo(before);
        await Assert.That(stored["Wallet"]["EnergyUsed"].AsInt32).IsEqualTo(0);
    }

    [Test]
    [Arguments(3, 0)]
    [Arguments(10, 7)]
    public async Task Exchange_PartialEnergy_FillsToTenWithOneDebitAsync(int crystals, int remainingCrystals)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28405);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28405, 9, crystals);
        var before = await GetStoredPlayerAsync(28405);

        // Act
        var result = await api.Player.ExchangeEnergyAsync(CancellationToken.None);
        var after = await GetStoredPlayerAsync(28405);

        // Assert
        await Assert.That(result.Exchanged).IsTrue();
        await Assert.That(result.Wallet).IsNotNull();
        await Assert.That(result.Wallet.Amount).IsEqualTo(10);
        await Assert.That(result.Wallet.Crystals).IsEqualTo(remainingCrystals);
        await Assert.That(result.Wallet.NextEnergyAtUtc).IsNull();
        await Assert.That(after["Wallet"]["EnergyUsed"]).IsEqualTo(before["Wallet"]["EnergyUsed"]);
        await Assert.That(after["Skills"]).IsEqualTo(before["Skills"]);
        await Assert.That(after["Settings"]).IsEqualTo(before["Settings"]);
        await Assert.That(after.Contains("EnergyExchangeRequestIds")).IsFalse();
    }

    [Test]
    [Arguments(10, 10, "energy_full")]
    [Arguments(9, 2, "insufficient_crystals")]
    public async Task Exchange_Unavailable_ReturnsExpectedResultWithoutDebitAsync(int energy, int crystals, string reason)
    {
        // Arrange
        using var api = Factory.CreateApiClient(28406);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28406, energy, crystals);
        var before = await GetStoredPlayerAsync(28406);

        // Act
        var result = await api.Player.ExchangeEnergyAsync(CancellationToken.None);
        var wallet = await ReadWalletAsync(api);
        var after = await GetStoredPlayerAsync(28406);

        // Assert
        await Assert.That(result.Exchanged).IsFalse();
        await Assert.That(result.Wallet).IsNull();
        await Assert.That(wallet.ExchangeQuote.Available).IsFalse();
        await Assert.That(wallet.ExchangeQuote.UnavailableReason).IsEqualTo(reason);
        await Assert.That(after).IsEqualTo(before);
    }

    [Test]
    public async Task Exchange_ConcurrentRequests_CommitOneDebitAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28407);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28407, 1, 10);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => api.Player.ExchangeEnergyAsync(CancellationToken.None)));
        var stored = await GetStoredPlayerAsync(28407);
        var next = await api.Player.ExchangeEnergyAsync(CancellationToken.None);

        // Assert
        await Assert.That(results.Count(result => result.Exchanged)).IsEqualTo(1);
        await Assert.That(next.Exchanged).IsFalse();
        await Assert.That(stored["Wallet"]["Energy"].AsInt32).IsEqualTo(10);
        await Assert.That(stored["Wallet"]["Crystals"].AsInt32).IsEqualTo(7);
    }

    [Test]
    public async Task Exchange_ProfileChanges_PreservesSkillsWithoutClientVersionAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28408);
        await CreatePlayerWithWorldAsync(api);
        await SetWalletAsync(28408, 5, 10);
        await Database.GetCollection<BsonDocument>("players").UpdateOneAsync(
            new BsonDocument("TelegramUserId", 28408L),
            new BsonDocument { ["$set"] = new BsonDocument("Skills.brave.Experience", 42), ["$inc"] = new BsonDocument("Version", 1) });
        var before = await GetStoredPlayerAsync(28408);

        // Act
        var result = await api.Player.ExchangeEnergyAsync(CancellationToken.None);
        var stored = await GetStoredPlayerAsync(28408);
        await SetWalletAsync(28408, 8, 7);
        var afterSpend = await api.Player.ExchangeEnergyAsync(CancellationToken.None);

        // Assert
        await Assert.That(result.Exchanged).IsTrue();
        await Assert.That(stored["Skills"]).IsEqualTo(before["Skills"]);
        await Assert.That(stored["Wallet"]["Crystals"].AsInt32).IsEqualTo(7);
        await Assert.That(afterSpend.Exchanged).IsTrue();
        await Assert.That(afterSpend.Wallet).IsNotNull();
        await Assert.That(afterSpend.Wallet.Crystals).IsEqualTo(4);
        await Assert.That((await api.Worlds.GetOwnedWorldsAsync(CancellationToken.None)).Count).IsEqualTo(1);
    }

    [Test]
    public async Task Publish_AfterFullIdle_StartsFreshRegenIntervalAsync()
    {
        // Arrange
        using var api = Factory.CreateApiClient(28409);
        await CreatePlayerWithWorldAsync(api);
        Factory.Session.Clock.Advance(TimeSpan.FromDays(1));
        await RestoreEnergyAsync();
        var spentAt = Factory.Session.Clock.GetUtcNow();

        // Act
        await PublishAsync(28409, "пост после полного запаса");
        var afterPost = await ReadWalletAsync(api);
        Factory.Session.Clock.Advance(TimeSpan.FromSeconds(599));
        await RestoreEnergyAsync();
        var beforeTick = await ReadWalletAsync(api);
        Factory.Session.Clock.Advance(TimeSpan.FromSeconds(1));
        await RestoreEnergyAsync();
        var afterTick = await ReadWalletAsync(api);

        // Assert
        await Assert.That(afterPost.Amount).IsEqualTo(9);
        await Assert.That(afterPost.NextEnergyAtUtc).IsEqualTo(spentAt.AddSeconds(600));
        await Assert.That(beforeTick.Amount).IsEqualTo(9);
        await Assert.That(afterTick.Amount).IsEqualTo(10);
        await Assert.That(afterTick.NextEnergyAtUtc).IsNull();
        await Assert.That((await GetStoredPlayerAsync(28409))["Wallet"]["EnergyUsed"].AsInt32).IsEqualTo(1);
    }

    private static async Task<EnergyWalletResponse> ReadWalletAsync(TestApiClient api) =>
        (await api.Player.GetCurrentPlayerAsync(CancellationToken.None)).EnergyWallet
        ?? throw new InvalidOperationException("The player response has no wallet.");

    private Task SetWalletAsync(long userId, int energy, int crystals) =>
        Database.GetCollection<PlayerProfile>("players").UpdateOneAsync(
            player => player.TelegramUserId == TelegramUserId.From(userId),
            Builders<PlayerProfile>.Update
                .Set(player => player.Wallet.Energy, energy)
                .Set(player => player.Wallet.Crystals, crystals)
                .Set(player => player.Wallet.EnergyUsed, 7)
                .Set(player => player.Wallet.EnergyUpdatedAtUtc, Factory.Session.Clock.GetUtcNow())
                .Inc(player => player.Version, 1));

    private async Task RestoreEnergyAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RestoreEnergyJob>().ExecuteAsync(new(), CancellationToken.None);
    }
}
