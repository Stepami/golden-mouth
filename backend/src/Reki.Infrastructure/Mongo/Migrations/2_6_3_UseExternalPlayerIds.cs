using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class UseExternalPlayerIds : RekiMigrationBase
{
    private const string LegacyIndexName = "ux_players_telegram_user";

    public override DatabaseVersion Version => new(2, 6, 3);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var players = db.GetCollection<BsonDocument>(MongoCollections.Players);
        var legacyPlayers = Builders<BsonDocument>.Filter.Exists("TelegramUserId");
        var externalId = new BsonDocument("$concat", new BsonArray
        {
            "telegram:",
            new BsonDocument("$toString", "$TelegramUserId"),
        });
        var initialize = new BsonDocument("$set", new BsonDocument
        {
            { "ExternalUserIds", new BsonArray { externalId } },
            { "OnboardingCompleted", true },
        });

        // Index DDL runs outside the migration transaction, so the backfill must be visible to it.
        // Every step is safe to retry if startup stops before the migration version is recorded.
        await players.UpdateManyAsync(
            legacyPlayers,
            Builders<BsonDocument>.Update.Pipeline(new[] { initialize }),
            cancellationToken: cancellationToken);

        var index = new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("ExternalUserIds"),
            new CreateIndexOptions { Name = "ux_players_external_user", Unique = true });
        await players.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);

        using var cursor = await players.Indexes.ListAsync(cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);
        if (indexes.Any(item => item["name"] == LegacyIndexName))
        {
            await players.Indexes.DropOneAsync(LegacyIndexName, cancellationToken);
        }

        await players.UpdateManyAsync(
            legacyPlayers,
            Builders<BsonDocument>.Update.Unset("TelegramUserId"),
            cancellationToken: cancellationToken);
    }
}
