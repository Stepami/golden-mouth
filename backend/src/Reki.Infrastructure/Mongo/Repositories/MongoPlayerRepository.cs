using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPlayerRepository : IPlayerRepository
{
    private readonly IMongoCollection<PlayerProfile> _players;

    public MongoPlayerRepository(IMongoDatabase database)
    {
        _players = database.GetCollection<PlayerProfile>(MongoCollections.Players);
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken) =>
        await _players.Find(item => item.TelegramUserId == telegramUserId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _players.Find(item => item.Id == playerId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<PlayerWallet?> TryExchangeEnergyAsync(
        PlayerId playerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.And(
            Builders<PlayerProfile>.Filter.Eq(item => item.Id, playerId),
            Builders<PlayerProfile>.Filter.Lt(item => item.Wallet.Energy, PlayerWallet.Cap),
            Builders<PlayerProfile>.Filter.Gte(item => item.Wallet.Crystals, PlayerWallet.ExchangeCost));
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.Wallet.Energy, PlayerWallet.Cap)
            .Inc(item => item.Wallet.Crystals, -PlayerWallet.ExchangeCost)
            .Set(item => item.Wallet.EnergyUpdatedAtUtc, now)
            .Inc(item => item.Version, 1);
        return await _players.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<PlayerProfile, PlayerWallet>
            {
                ReturnDocument = ReturnDocument.Before,
                Projection = Builders<PlayerProfile>.Projection.Expression(item => item.Wallet),
            },
            cancellationToken);
    }

    public async IAsyncEnumerable<PlayerProfile> GetPlayersForEnergyRecoveryAsync(
        DateTimeOffset dueBefore, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var cursor = await _players.FindAsync(
            item => item.Wallet.Energy < PlayerWallet.Cap && item.Wallet.EnergyUpdatedAtUtc <= dueBefore,
            cancellationToken: cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return player;
        }
    }

    public async ValueTask<bool> TrySaveEnergyRecoveryAsync(
        PlayerProfile player, PlayerWallet wallet, CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.Wallet.Energy, wallet.Energy)
            .Set(item => item.Wallet.EnergyUpdatedAtUtc, wallet.EnergyUpdatedAtUtc)
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
            .Set(item => item.Wallet.Energy, updated.Wallet.Energy)
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
            .Set(player => player.Wallet.EnergyUsed, 0)
            .Set(player => player.PostsPeriodStartedAtUtc, periodStart)
            .Inc(player => player.Version, 1);
        var result = await _players.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }
}
