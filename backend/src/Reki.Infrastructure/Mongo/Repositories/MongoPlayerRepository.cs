using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPlayerRepository : IPlayerRepository
{
    private readonly IMongoCollection<PlayerProfile> _players;

    private readonly TimeProvider _timeProvider;

    public MongoPlayerRepository(IMongoDatabase database, TimeProvider timeProvider)
    {
        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
        _timeProvider = timeProvider;
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken) =>
        await GetWithEnergyAsync(Builders<PlayerProfile>.Filter.Eq(item => item.TelegramUserId, telegramUserId), cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await GetWithEnergyAsync(Builders<PlayerProfile>.Filter.Eq(item => item.Id, playerId), cancellationToken);

    public async ValueTask<bool> TrySaveWalletAsync(PlayerProfile player, PlayerProfile updated, CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.Energy, updated.Energy)
            .Set(item => item.EnergyUpdatedAtUtc, updated.EnergyUpdatedAtUtc)
            .Set(item => item.Crystals, updated.Crystals)
            .Set(item => item.EnergyExchangeRequestIds, updated.EnergyExchangeRequestIds)
            .Inc(item => item.Version, 1);
        var result = await _players.UpdateOneAsync(
            item => item.Id == player.Id && item.Version == player.Version,
            update,
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async ValueTask CreatePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.InsertOneAsync(player, cancellationToken: cancellationToken);

    public async ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.And(
            Builders<PlayerProfile>.Filter.Eq(item => item.Id, player.Id),
            Builders<PlayerProfile>.Filter.Ne(item => item.PlusNotifyRequested, updated.PlusNotifyRequested));
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.PlusNotifyRequested, updated.PlusNotifyRequested)
            .Inc(item => item.Version, updated.Version - player.Version);
        await _players.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async ValueTask<bool> TryClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.And(
            Builders<PlayerProfile>.Filter.Eq(item => item.Id, player.Id),
            Builders<PlayerProfile>.Filter.Eq(item => item.Version, player.Version),
            Builders<PlayerProfile>.Filter.Ne(item => item.PlusInterestClaimed, updated.PlusInterestClaimed));
        var update = Builders<PlayerProfile>.Update
            .Inc(item => item.Energy, updated.Energy - player.Energy)
            .Set(item => item.PlusInterestClaimed, updated.PlusInterestClaimed)
            .Inc(item => item.Version, updated.Version - player.Version);
        var result = await _players.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async ValueTask<bool> TryReplacePlayerAsync(PlayerProfile player, int expectedVersion, CancellationToken cancellationToken)
    {
        var result = await _players.ReplaceOneAsync(item => item.Id == player.Id && item.Version == expectedVersion, player, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken) =>
        await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);

    public async ValueTask<long> ResetGameDayAsync(
        DateTimeOffset periodStart,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.Lt(player => player.PostsPeriodStartedAtUtc, periodStart);
        var update = Builders<PlayerProfile>.Update
            .Set(player => player.EnergyUsed, 0)
            .Set(player => player.PostsPeriodStartedAtUtc, periodStart)
            .Inc(player => player.Version, 1);
        var result = await _players.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    private async ValueTask<PlayerProfile?> GetWithEnergyAsync(FilterDefinition<PlayerProfile> filter, CancellationToken cancellationToken)
    {
        while (true)
        {
            var player = await _players.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (player is null)
            {
                return null;
            }

            var updated = EnergyWallet.Refresh(player, _timeProvider.GetUtcNow());
            if (ReferenceEquals(player, updated) || await TrySaveWalletAsync(player, updated, cancellationToken))
            {
                return updated;
            }
        }
    }
}
