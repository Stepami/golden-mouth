using MongoDB.Driver;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure.Worlds;

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
        return await _templates.Find(FilterDefinition<World>.Empty).ToListAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<World>> GetWorldsAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        return await _worlds.Find(item => item.Player.PlayerId == playerId).ToListAsync(cancellationToken);
    }
}
