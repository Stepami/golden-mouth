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
    private readonly MongoSessionProvider _sessionProvider;

    public MongoWorldRepository(IMongoDatabase database, MongoSessionProvider sessionProvider)
    {
        _worlds = database.GetCollection<World>(MongoCollections.Worlds);
        _templates = database.GetCollection<World>(MongoCollections.WorldTemplates);
        _sessionProvider = sessionProvider;
    }

    public async ValueTask InsertWorldAsync(World world, CancellationToken cancellationToken) =>
        await _worlds.InsertOneAsync(_sessionProvider, world, cancellationToken);

    public async ValueTask<bool> TryReplaceWorldAsync(World world, int expectedVersion, CancellationToken cancellationToken)
    {
        var result = await _worlds.ReplaceOneAsync(_sessionProvider, item => item.Id == world.Id && item.Player.PlayerId == world.Player.PlayerId && item.Version == expectedVersion, world, cancellationToken);
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

    public async ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        return await _worlds.Find(item => item.Player.PlayerId == playerId).ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<WorldId>> GetWorldIdsAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _worlds.Find(_sessionProvider.Session, item => item.Player.PlayerId == playerId)
            .Project(item => item.Id).ToListAsync(cancellationToken);

    public async ValueTask DeleteWorldsAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _worlds.DeleteManyAsync(_sessionProvider.Session, item => item.Player.PlayerId == playerId, cancellationToken: cancellationToken);

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
