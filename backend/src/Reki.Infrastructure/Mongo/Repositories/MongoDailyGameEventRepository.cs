using MongoDB.Driver;
using Reki.Application.Events;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoDailyGameEventRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IDailyGameEventRepository
{
    private readonly IMongoCollection<DailyGameEvent> _events =
        database.GetCollection<DailyGameEvent>(MongoCollections.Events);

    public async ValueTask<DailyGameEvent?> GetTodayEventAsync(
        PlayerId playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken,
        WorldId worldId)
    {
        var filter = Builders<DailyGameEvent>.Filter
                         .Eq(item => item.PlayerId, playerId)
                     & Builders<DailyGameEvent>.Filter.Eq(item => item.WorldId, worldId);

        if (dayStartedAtUtc != DateTimeOffset.MinValue)
        {
            var dayFilter = Builders<DailyGameEvent>.Filter
                .Eq(item => item.DayStartedAtUtc, dayStartedAtUtc);
            filter = Builders<DailyGameEvent>.Filter.And(filter, dayFilter);
        }

        return await _events
            .Find(filter)
            .SortByDescending(item => item.DayStartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask SaveEventAsync(
        DailyGameEvent gameEvent,
        CancellationToken cancellationToken)
    {
        var options = new ReplaceOptions { IsUpsert = true };
        var filter = Builders<DailyGameEvent>.Filter.Eq(item => item.Id, gameEvent.Id);
        await _events.ReplaceOneAsync(filter, gameEvent, options, cancellationToken);
    }

    public async ValueTask DeleteEventsAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _events.DeleteManyAsync(sessionProvider.Session, item => item.PlayerId == playerId, cancellationToken: cancellationToken);
}
