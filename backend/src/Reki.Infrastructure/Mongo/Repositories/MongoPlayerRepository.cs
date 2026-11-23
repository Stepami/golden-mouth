using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.UnitOfWork;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoPlayerRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IPlayerRepository
{
    private readonly IMongoCollection<PlayerProfile> _players =
        database.GetCollection<PlayerProfile>(MongoCollections.Players);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        ExternalUserId externalUserId,
        CancellationToken cancellationToken) =>
        await _players.Find(sessionProvider, item => item.ExternalUserIds.Contains(externalUserId))
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        PlayerId playerId,
        CancellationToken cancellationToken) =>
        await _players.Find(sessionProvider, item => item.Id == playerId)
            .FirstOrDefaultAsync(cancellationToken);

    public async IAsyncEnumerable<PlayerProfile> GetPlayersForEnergyRecoveryAsync(
        int cap,
        DateTimeOffset dueBefore,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var cursor = await _players.FindAsync(
            item => item.Wallet.Energy < cap && item.Wallet.EnergyUpdatedAtUtc <= dueBefore,
            cancellationToken: cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return player;
        }
    }

    public async IAsyncEnumerable<PlayerProfile> GetActivePlayersAsync(
        DateTimeOffset since,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.Gte(item => item.LastSeenAt, since);
        using var cursor = await _players.FindAsync(
            filter,
            cancellationToken: cancellationToken);
        await foreach (var player in cursor.ToAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return player;
        }
    }

    public async ValueTask CreatePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.InsertOneAsync(sessionProvider.Session, player, cancellationToken: cancellationToken);

    public async ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        int previousVersion,
        CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.PlusNotifyRequested, player.PlusNotifyRequested)
            .Inc(item => item.Version, player.Version - previousVersion);
        await _players.UpdateOneAsync(item => item.Id == player.Id, update, cancellationToken: cancellationToken);
    }

    public async ValueTask<int> ClaimPlusInterestAsync(
        PlayerProfile player,
        int previousVersion,
        int previousCrystals,
        CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Inc(item => item.Wallet.Crystals, player.Wallet.Crystals - previousCrystals)
            .Set(item => item.PlusInterestClaimed, player.PlusInterestClaimed)
            .Inc(item => item.Version, player.Version - previousVersion);
        var result = await _players.UpdateOneAsync(
            item => item.Id == player.Id && !item.PlusInterestClaimed && item.Version == previousVersion,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1 ? player.Wallet.Crystals - previousCrystals : 0;
    }

    public async ValueTask ReplacePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.ReplaceOneAsync(sessionProvider, item => item.Id == player.Id, player, cancellationToken);

    public async ValueTask SaveDailyChallengeAsync(PlayerProfile player, int previousVersion, CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.Eq(item => item.Id, player.Id);
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.DailyChallenge, player.DailyChallenge)
            .Inc(item => item.Version, player.Version - previousVersion);
        await _players.UpdateOneAsync(sessionProvider, filter, update, cancellationToken);
    }

    public async ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _players.DeleteOneAsync(sessionProvider.Session, item => item.Id == playerId, cancellationToken: cancellationToken);

    public async ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken) =>
        await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);
}
