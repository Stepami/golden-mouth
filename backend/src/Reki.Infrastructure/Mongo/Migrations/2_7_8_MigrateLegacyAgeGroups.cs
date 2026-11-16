using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class MigrateLegacyAgeGroups : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 7, 8);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        var year = DateTime.UtcNow.Year;
        var branches = new BsonArray
        {
            new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { "$AgeBand", "18-24" }) }, { "then", 21 } },
            new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { "$AgeBand", "25-34" }) }, { "then", 30 } },
            new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { "$AgeBand", "35-44" }) }, { "then", 40 } },
            new BsonDocument { { "case", new BsonDocument("$eq", new BsonArray { "$AgeBand", "45+" }) }, { "then", 45 } },
        };
        var age = new BsonDocument("$switch", new BsonDocument
        {
            { "branches", branches },
            { "default", 18 },
        });
        var birthYear = new BsonDocument("$min", new BsonArray
        {
            new BsonDocument("$ifNull", new BsonArray { "$BirthYear", new BsonDocument("$subtract", new BsonArray { year, age }) }),
            year - 18,
        });
        var pipeline = new[]
        {
            new BsonDocument("$set", new BsonDocument { { "BirthYear", birthYear }, { "Version", new BsonDocument("$add", new BsonArray { new BsonDocument("$ifNull", new BsonArray { "$Version", 0 }), 1 }) } }),
            new BsonDocument("$unset", new BsonArray { "BirthGroup", "AgeBand" }),
        };
        await players.UpdateManyAsync(session, FilterDefinition<BsonDocument>.Empty, new PipelineUpdateDefinition<BsonDocument>(pipeline), cancellationToken: cancellationToken);
    }
}
