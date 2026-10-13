using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed partial class RefreshWorldPresets : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 2, 1);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var templates = db.GetCollection<World>(MongoCollections.WorldTemplates);
        await templates.DeleteManyAsync(session, Builders<World>.Filter.Ne(item => item.Id, WorldId.From("it")), cancellationToken: cancellationToken);
        await templates.InsertManyAsync(session, CreateReleaseTemplates(), cancellationToken: cancellationToken);
    }
}
