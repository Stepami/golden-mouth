using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class MigrateLegacyAgeGroups : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 8);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<PlayerProfile>(MongoCollections.Players);
        var filter = Builders<PlayerProfile>.Filter.Where(player => player.BirthGroup == "14-17" || player.AgeBand == "14-17");
        var update = Builders<PlayerProfile>.Update
            .Set(player => player.BirthGroup, "18+")
            .Set(player => player.AgeBand, null)
            .Inc(player => player.Version, 1);
        await players.UpdateManyAsync(session, filter, update, cancellationToken: cancellationToken);
    }
}
