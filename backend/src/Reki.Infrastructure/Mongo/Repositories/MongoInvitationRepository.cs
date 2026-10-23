using MongoDB.Driver;
using Reki.Application.Invites;
using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Infrastructure.Mongo.Repositories;

internal sealed class MongoInvitationRepository : IInvitationRepository
{
    private readonly IMongoCollection<Invitation> _invites;

    public MongoInvitationRepository(MongoCompositionRoot compositionRoot)
    {
        _invites = compositionRoot.MongoDatabase.GetCollection<Invitation>(MongoCollections.Invites);
    }

    public async ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken) =>
        await _invites.InsertOneAsync(invite, cancellationToken: cancellationToken);

    public async ValueTask<Invitation?> GetPendingInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken) =>
        await _invites.Find(item => item.InviteeId == inviteeId && !item.InviteeBonusUsed)
            .FirstOrDefaultAsync(cancellationToken);
}
