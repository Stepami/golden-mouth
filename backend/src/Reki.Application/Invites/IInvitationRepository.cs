using Reki.Domain.Invites;
using Reki.Domain.Player;

namespace Reki.Application.Invites;

public interface IInvitationRepository
{
    ValueTask CreateInviteAsync(Invitation invite, CancellationToken cancellationToken);

    ValueTask<Invitation?> GetPendingInviteAsync(PlayerId inviteeId, CancellationToken cancellationToken);
}
