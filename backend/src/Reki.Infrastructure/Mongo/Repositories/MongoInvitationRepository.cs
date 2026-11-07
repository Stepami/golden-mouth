using MongoDB.Driver;
using Reki.Application.Invites;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoInvitationRepository : IInvitationRepository
{
    private readonly IMongoCollection<Invitation> _invites;
    private readonly MongoSessionProvider _sessionProvider;

    public MongoInvitationRepository(IMongoDatabase database, MongoSessionProvider sessionProvider)
    {
        _invites = database.GetCollection<Invitation>(MongoCollections.Invites);
        _sessionProvider = sessionProvider;
    }

    public async ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken) =>
        await _invites.InsertOneAsync(invite, cancellationToken: cancellationToken);

    public async ValueTask<Invitation?> GetPendingInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken) =>
        await _invites.Find(item => item.InviteeId == inviteeId && !item.InviteeBonusUsed)
            .FirstOrDefaultAsync(cancellationToken);

    public async ValueTask<long> CountInviterBonusesAsync(
        PlayerId inviterId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken) =>
        await _invites.CountDocumentsAsync(
            _sessionProvider.Session,
            item => item.InviterId == inviterId && item.InviterBonusUsed &&
                    item.InviterBonusUsedAtUtc >= periodStart && item.InviterBonusUsedAtUtc < periodEnd,
            cancellationToken: cancellationToken);

    public async ValueTask UseInviteBonusAsync(Invitation invite, CancellationToken cancellationToken)
    {
        var update = Builders<Invitation>.Update
            .Set(item => item.InviterBonusUsed, invite.InviterBonusUsed)
            .Set(item => item.InviteeBonusUsed, invite.InviteeBonusUsed)
            .Set(item => item.InviterBonusUsedAtUtc, invite.InviterBonusUsedAtUtc)
            .Set(item => item.InviteeBonusUsedAtUtc, invite.InviteeBonusUsedAtUtc);
        await _invites.UpdateOneAsync(
            _sessionProvider.Session,
            item => item.Id == invite.Id,
            update,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DeleteInvitesAsync(PlayerId playerId, CancellationToken cancellationToken) =>
        await _invites.DeleteManyAsync(
            _sessionProvider.Session,
            item => item.InviterId == playerId || item.InviteeId == playerId,
            cancellationToken: cancellationToken);
}
