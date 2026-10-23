using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoWorldRepository : IWorldRepository
{
    private readonly IMongoCollection<World> _worlds;
    private readonly IMongoCollection<World> _templates;

    public MongoWorldRepository(MongoCompositionRoot compositionRoot)
    {
        _worlds = compositionRoot.MongoDatabase.GetCollection<World>(MongoCollections.Worlds);
        _templates = compositionRoot.MongoDatabase.GetCollection<World>(MongoCollections.WorldTemplates);
    }

    public async ValueTask InsertWorldAsync(World world, CancellationToken cancellationToken) =>
        await _worlds.InsertOneAsync(world, cancellationToken: cancellationToken);

    public async ValueTask<bool> TryReplaceWorldAsync(World world, int expectedVersion, CancellationToken cancellationToken)
    {
        var result = await _worlds.ReplaceOneAsync(item => item.Id == world.Id && item.Player.PlayerId == world.Player.PlayerId && item.Version == expectedVersion, world, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async ValueTask<World?> GetWorldAsync(PlayerId playerId, WorldId worldId, CancellationToken cancellationToken)
    {
        return await _worlds.Find(item => item.Player.PlayerId == playerId && item.Id == worldId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<World>> GetWorldTemplatesAsync(CancellationToken cancellationToken)
    {
        return await _templates.Find(item => item.Player.PlayerId == PlayerId.From(Guid.Empty) &&
            item.TemplateId == null && !item.IsArchived).ToListAsync(cancellationToken);
    }

    public async ValueTask<World?> GetWorldTemplateAsync(WorldId templateId, CancellationToken cancellationToken) =>
        await _templates.Find(item => item.Id == templateId && item.Player.PlayerId == PlayerId.From(Guid.Empty) &&
            item.TemplateId == null).FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        return await _worlds.Find(item => item.Player.PlayerId == playerId).ToListAsync(cancellationToken);
    }

    public async ValueTask<long> ResetGameDayAsync(
        DateTimeOffset periodStart,
        DateTimeOffset restedBefore,
        int restedPosts,
        CancellationToken cancellationToken)
    {
        var filter = Builders<World>.Filter.Lt(world => world.Player.PeriodStartedAtUtc, periodStart);
        var rested = Builders<World>.Filter.Ne(world => world.Player.LastPostAtUtc, null) &
                     Builders<World>.Filter.Lt(world => world.Player.LastPostAtUtc, restedBefore);
        var update = Builders<World>.Update
            .Set(world => world.Player.PeriodStartedAtUtc, periodStart)
            .Set(world => world.Player.Wants.AllElements().Done, false)
            .Set(world => world.Stars.AllElements().RelationGainedInPeriod, 0)
            .Set(world => world.Stars.AllElements().RelationPeriodStartedAtUtc, periodStart)
            .Inc(world => world.Version, 1);
        var returning = await _worlds.UpdateManyAsync(
            filter & rested,
            update.Set(world => world.Player.RestedPostsRemaining, restedPosts),
            cancellationToken: cancellationToken);
        var active = await _worlds.UpdateManyAsync(
            filter & Builders<World>.Filter.Not(rested),
            update.Set(world => world.Player.RestedPostsRemaining, 0),
            cancellationToken: cancellationToken);
        return returning.ModifiedCount + active.ModifiedCount;
    }
}
