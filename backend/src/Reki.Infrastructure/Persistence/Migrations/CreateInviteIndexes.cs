using Kot.MongoDB.Migrations;
using MongoDB.Bson;
using MongoDB.Driver;
using Reki.Domain.Invites;

namespace Reki.Infrastructure.Persistence.Migrations;

internal sealed class CreateInviteIndexes : RekiMigrationBase
{
    public override DatabaseVersion Version => new(2, 1, 0);

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        var invites = db.GetCollection<BsonDocument>(MongoCollections.Invites);
        var inviteeIndex = new CreateIndexModel<BsonDocument>(
            new BsonDocument(nameof(Invitation.InviteeId), 1),
            new CreateIndexOptions<BsonDocument>
            {
                Name = "ux_invites_invitee",
                Unique = true,
                PartialFilterExpression = new BsonDocument("InviteeId", new BsonDocument("$type", "string")),
            });
        var inviterIndex = new CreateIndexModel<BsonDocument>(
            new BsonDocument(nameof(Invitation.InviterId), 1),
            new CreateIndexOptions { Name = "ix_invites_inviter" });
        await invites.Indexes.CreateManyAsync([inviteeIndex, inviterIndex], cancellationToken);
    }

    public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Adopted invitation indexes cannot be safely rolled back.");
}