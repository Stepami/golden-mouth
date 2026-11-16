using MongoDB.Driver;
using Reki.Application.Invites;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoInvitationRepository(
    IMongoDatabase database,
    MongoSessionProvider sessionProvider) : IInvitationRepository
{
    private readonly IMongoCollection<Invitation> _invites =
        database.GetCollection<Invitation>(MongoCollections.Invites);

    public async ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken) =>
        await _invites.InsertOneAsync(sessionProvider.Session, invite, cancellationToken: cancellationToken);

    public async ValueTask<Invitation?> GetPendingInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken) =>
        await _invites.Find(item => item.InviteeId == inviteeId && !item.InviteeBonusUsed)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask UseInviteBonusAsync(Invitation invite, CancellationToken cancellationToken)
    {
        var update = Builders<Invitation>.Update
            .Set(item => item.InviterBonusUsed, invite.InviterBonusUsed)
            .Set(item => item.InviteeBonusUsed, invite.InviteeBonusUsed)
            .Set(item => item.InviterBonusUsedAtUtc, invite.InviterBonusUsedAtUtc)
            .Set(item => item.InviteeBonusUsedAtUtc, invite.InviteeBonusUsedAtUtc)
            .Set(item => item.Version, invite.Version);
        await _invites.UpdateOneAsync(
            sessionProvider.Session,
            item => item.Id == invite.Id,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DeleteInvitesAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _invites.DeleteManyAsync(
            sessionProvider.Session,
            item => item.InviterId == playerId || item.InviteeId == playerId,
            cancellationToken: cancellationToken);
}
