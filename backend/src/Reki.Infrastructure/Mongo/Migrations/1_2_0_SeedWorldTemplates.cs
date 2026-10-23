using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed partial class SeedWorldTemplates : RekiMigrationBase
{
    public override DatabaseVersion Version => new(1, 2, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var templates = db.GetCollection<BsonDocument>(MongoCollections.WorldTemplates);
        foreach (var template in CreateTemplates())
        {
            var document = template.ToBsonDocument();

            // Migration 2.0.0 still consumes the historical key; 2.5.0 removes it.
            document["BalanceKey"] = template.Id.Value.ToString("D") switch
            {
                "edfccf53-65c6-4eaf-8321-2391bc1758bd" => "stage",
                "b018145e-c887-4d00-9f18-5c877d0af8be" => "academy",
                "5720946a-ea7c-4a10-9c50-51ce3deefc53" => "it",
                "0cb3f809-6dfd-4ef1-af52-daa12a5b8804" => "stream",
                "14049891-fe2b-4d98-b38f-0f37e6544052" => "own",
                _ => throw new InvalidOperationException("Unknown historical template."),
            };
            if (!await templates.Find(session, new BsonDocument("_id", document["_id"])).AnyAsync(cancellationToken))
            {
                await templates.InsertOneAsync(session, document, cancellationToken: cancellationToken);
            }
        }
    }
}
