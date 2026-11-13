using MongoDB.Driver;
using Reki.Application.Worlds;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoWorldRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IWorldRepository
{
    private readonly IMongoCollection<World> _worlds =
        database.GetCollection<World>(MongoCollections.Worlds);

    private readonly IMongoCollection<World> _templates =
        database.GetCollection<World>(MongoCollections.WorldTemplates);

    public async ValueTask InsertWorldAsync(World world, CancellationToken cancellationToken) =>
        await _worlds.InsertOneAsync(world, cancellationToken: cancellationToken);

    public async ValueTask ReplaceWorldAsync(World world, CancellationToken cancellationToken) =>
        await _worlds.ReplaceOneAsync(
            sessionProvider,
            item => item.Id == world.Id && item.Player.PlayerId == world.Player.PlayerId,
            world,
            cancellationToken);

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
        await _worlds.Find(sessionProvider.Session, item => item.Player.PlayerId == playerId)
            .Project(item => item.Id).ToListAsync(cancellationToken);

    public async ValueTask DeleteWorldsAsync(IReadOnlyList<WorldId> worldIds, CancellationToken cancellationToken) =>
        await _worlds.DeleteManyAsync(sessionProvider.Session, Builders<World>.Filter.In(item => item.Id, worldIds), cancellationToken: cancellationToken);
}
