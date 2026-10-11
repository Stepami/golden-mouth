using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed partial class SeedWorldTemplates : RekiMigrationBase
{
    public override DatabaseVersion Version => new(1, 2, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var templates = db.GetCollection<World>(MongoCollections.WorldTemplates);
        foreach (var template in CreateTemplates())
        {
            if (!await templates.Find(session, item => item.Id == template.Id).AnyAsync(cancellationToken))
            {
                await templates.InsertOneAsync(session, template, cancellationToken: cancellationToken);
            }
        }
    }

    public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Seeded templates may have been edited.");
}