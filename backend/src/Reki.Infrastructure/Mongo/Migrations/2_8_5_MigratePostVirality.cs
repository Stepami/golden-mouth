using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Posts;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class MigratePostVirality : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 8, 5);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var posts = db.GetCollection<BsonDocument>(MongoCollections.Posts);
        var filters = Builders<BsonDocument>.Filter;
        var updates = Builders<BsonDocument>.Update;
        var missingVirality = filters.Exists(nameof(GamePost.Virality), false);

        foreach (var (filter, status) in new[]
                 {
                     (filters.Eq("Viral", true), PostViralityStatus.Viral),
                     (filters.Eq("Flop", true), PostViralityStatus.Flop),
                     (FilterDefinition<BsonDocument>.Empty, PostViralityStatus.None),
                 })
        {
            await posts.UpdateManyAsync(
                session,
                missingVirality & filter,
                updates.Set(nameof(GamePost.Virality), (int)status).Unset("Viral").Unset("Flop"),
                cancellationToken: cancellationToken);
        }
    }
}
