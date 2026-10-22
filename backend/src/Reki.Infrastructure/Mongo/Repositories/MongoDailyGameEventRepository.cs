using MongoDB.Driver;
using Reki.Application.Events;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoDailyGameEventRepository : IDailyGameEventRepository
{
    private readonly IMongoCollection<DailyGameEvent> _events;

    public MongoDailyGameEventRepository(MongoCompositionRoot compositionRoot)
    {
        _events = compositionRoot.MongoDatabase.GetCollection<DailyGameEvent>(MongoCollections.Events);
    }

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
        await _events.ReplaceOneAsync(
            item => item.Id == gameEvent.Id,
            gameEvent,
            options,
            cancellationToken);
    }
}
