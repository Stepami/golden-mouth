using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Player;

internal sealed class MongoPlayerRepository : IPlayerRepository
{
    private readonly IMongoCollection<PlayerProfile> _players;

    public MongoPlayerRepository(MongoCompositionRoot compositionRoot)
    {
        _players = compositionRoot.MongoDatabase.GetCollection<PlayerProfile>(MongoCollections.Players);
    }

    public async ValueTask<PlayerProfile?> GetPlayerAsync(
        TelegramUserId telegramUserId,
        CancellationToken cancellationToken) =>
        await _players.Find(item => item.TelegramUserId == telegramUserId).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<PlayerProfile?> GetPlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _players.Find(item => item.Id == playerId).FirstOrDefaultAsync(cancellationToken);

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
}
