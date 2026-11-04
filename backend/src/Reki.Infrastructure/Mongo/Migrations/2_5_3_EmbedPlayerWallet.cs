using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Player;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class EmbedPlayerWallet(TimeProvider timeProvider) : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 5, 3);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        await db.GetCollection<PlayerProfile>(MongoCollections.Players).Indexes.CreateOneAsync(
            new CreateIndexModel<PlayerProfile>(
                Builders<PlayerProfile>.IndexKeys.Ascending(player => player.Wallet.EnergyUpdatedAtUtc),
                new CreateIndexOptions<PlayerProfile>
                {
                    Name = "ix_players_wallet_recovery",
                    PartialFilterExpression = Builders<PlayerProfile>.Filter.Lt(player => player.Wallet.Energy, PlayerWallet.Cap),
                }),
            cancellationToken: cancellationToken);

        var wallet = PlayerWallet.Create(timeProvider.GetUtcNow()).ToBsonDocument();
        wallet[nameof(PlayerWallet.Energy)] = new BsonDocument("$min", new BsonArray
        {
            PlayerWallet.Cap,
            new BsonDocument("$max", new BsonArray { 0, IfNull("$Energy", 0) }),
        });
        wallet[nameof(PlayerWallet.EnergyUsed)] = IfNull("$EnergyUsed", 0);
        wallet[nameof(PlayerWallet.Crystals)] = IfNull("$Crystals", PlayerWallet.InitialCrystals);
        var set = new BsonDocument("$set", new BsonDocument
        {
            [nameof(PlayerProfile.Wallet)] = wallet,
            [nameof(PlayerProfile.Version)] = new BsonDocument("$add", new BsonArray { IfNull("$Version", 0), 1 }),
        });
        var unset = new BsonDocument("$unset", new BsonArray
        {
            "Energy", "EnergyUsed", "Crystals", "EnergyUpdatedAtUtc", "EnergyExchangeRequestIds",
        });
        await db.GetCollection<BsonDocument>(MongoCollections.Players).UpdateManyAsync(
            session,
            Builders<BsonDocument>.Filter.Exists(nameof(PlayerProfile.Wallet), false),
            Builders<BsonDocument>.Update.Pipeline(new[] { set, unset }),
            cancellationToken: cancellationToken);
    }

    private static BsonDocument IfNull(string field, BsonValue fallback) => new("$ifNull", new BsonArray { field, fallback });
}
