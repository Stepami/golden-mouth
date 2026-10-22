using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Application.Balance;
using Reki.Domain.Invites;
using Reki.Infrastructure.Mongo.Repositories;

namespace Reki.Infrastructure.Mongo.Migrations;

internal sealed class AddBonusAmountToInvite(IOptions<GameBalance> balance) : RekiMigrationBase
{
    public override DatabaseVersion Version { get; } = new(2, 4, 1);

    public override async Task UpAsync(
        IMongoDatabase db,
        IClientSessionHandle session,
        CancellationToken cancellationToken)
    {
        await db.GetCollection<BsonDocument>(MongoCollections.Invites).UpdateManyAsync(
            session,
            FilterDefinition<BsonDocument>.Empty,
            Builders<BsonDocument>.Update.Set(nameof(Invitation.BonusAmount), balance.Value.InviteBonusBoth),
            cancellationToken: cancellationToken);
    }
}