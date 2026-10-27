using MongoDB.Driver;
using Reki.Application.Player;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

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

    public async ValueTask<long> CountPlayersAsync(CancellationToken cancellationToken) =>
        await _players.CountDocumentsAsync(FilterDefinition<PlayerProfile>.Empty, cancellationToken: cancellationToken);

    public async ValueTask<long> ResetGameDayAsync(
        DateTimeOffset periodStart,
        int freeEnergy,
        int plusEnergy,
        CancellationToken cancellationToken)
    {
        var filter = Builders<PlayerProfile>.Filter.Lt(player => player.PostsPeriodStartedAtUtc, periodStart);
        var update = Builders<PlayerProfile>.Update
            .Set(player => player.EnergyUsed, 0)
            .Set(player => player.PostsPeriodStartedAtUtc, periodStart)
            .Inc(player => player.Version, 1);
        var free = await _players.UpdateManyAsync(
            filter & Builders<PlayerProfile>.Filter.Ne(player => player.Settings.Plus, true),
            update.Set(player => player.Energy, freeEnergy),
            cancellationToken: cancellationToken);
        var plus = await _players.UpdateManyAsync(
            filter & Builders<PlayerProfile>.Filter.Eq(player => player.Settings.Plus, true),
            update.Set(player => player.Energy, plusEnergy),
            cancellationToken: cancellationToken);
        return free.ModifiedCount + plus.ModifiedCount;
    }
}
