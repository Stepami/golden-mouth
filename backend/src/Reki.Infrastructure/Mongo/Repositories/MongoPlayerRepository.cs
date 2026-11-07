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
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.PlusNotifyRequested, updated.PlusNotifyRequested)
            .Inc(item => item.Version, updated.Version - player.Version);
        await _players.UpdateOneAsync(item => item.Id == player.Id, update, cancellationToken: cancellationToken);
    }

    public async ValueTask ClaimPlusInterestAsync(
        PlayerProfile player,
        PlayerProfile updated,
        CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Inc(item => item.Energy, updated.Energy - player.Energy)
            .Set(item => item.PlusInterestClaimed, updated.PlusInterestClaimed)
            .Inc(item => item.Version, updated.Version - player.Version);
        await _players.UpdateOneAsync(item => item.Id == player.Id, update, cancellationToken: cancellationToken);
    }

    public async ValueTask ReplacePlayerAsync(PlayerProfile player, CancellationToken cancellationToken) =>
        await _players.ReplaceOneAsync(sessionProvider, item => item.Id == player.Id, player, cancellationToken);

    public async ValueTask AddInviteBonusAsync(PlayerId playerId, int bonusAmount, CancellationToken cancellationToken)
    {
        var update = Builders<PlayerProfile>.Update
            .Set(item => item.HitOwnWorldMilestone, true)
            .Inc(item => item.Energy, bonusAmount)
            .Inc(item => item.Version, 1);
        await _players.UpdateOneAsync(
            sessionProvider.Session,
            item => item.Id == playerId,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _players.DeleteOneAsync(sessionProvider.Session, item => item.Id == playerId, cancellationToken: cancellationToken);

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
