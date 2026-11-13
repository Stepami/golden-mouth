using System.Runtime.CompilerServices;
using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;

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
        await _players.Find(item => item.ExternalUserIds.Contains(externalUserId)).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await (sessionProvider.Current is { } session
            ? _players.Find(session, item => item.Id == playerId)
            : _players.Find(item => item.Id == playerId)).FirstOrDefaultAsync(cancellationToken);

    public async IAsyncEnumerable<PlayerProfile> GetPlayersForEnergyRecoveryAsync(
        int cap, DateTimeOffset dueBefore, [EnumeratorCancellation] CancellationToken cancellationToken)
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

    public async ValueTask CreatePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.InsertOneAsync(sessionProvider.Session, player, cancellationToken: cancellationToken);

    public async ValueTask SavePlusNotificationAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.PlusNotifyRequested, updated.PlusNotifyRequested)
            .Inc(item => item.Version, updated.Version - player.Version);
        await _players.UpdateOneAsync(item => item.Id == player.Id, update, cancellationToken: cancellationToken);
    }

    public async ValueTask<int> ClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Inc(item => item.Wallet.Energy, updated.Wallet.Energy - player.Wallet.Energy)
            .Set(item => item.PlusInterestClaimed, updated.PlusInterestClaimed)
            .Inc(item => item.Version, updated.Version - player.Version);
        var result = await _players.UpdateOneAsync(
            item => item.Id == player.Id && !item.PlusInterestClaimed && item.Version == player.Version,
            update,
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1 ? updated.Wallet.Energy - player.Wallet.Energy : 0;
    }

    public async ValueTask ReplacePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.ReplaceOneAsync(sessionProvider, item => item.Id == player.Id, player, cancellationToken);

    public async ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _players.DeleteOneAsync(sessionProvider.Session, item => item.Id == playerId, cancellationToken: cancellationToken);

    public async ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken) =>
        await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);
}
