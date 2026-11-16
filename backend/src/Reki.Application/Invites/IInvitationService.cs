using Reki.Domain.Player;

namespace Reki.Application.Invites;

public interface IInvitationService
{
    ValueTask<int> GetBonusAsync(PlayerId playerId, CancellationToken cancellationToken);

    void ClearPlayer(PlayerId playerId);
}
