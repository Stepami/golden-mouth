using Microsoft.Extensions.Caching.Memory;
using Reki.Domain.Player;

namespace Reki.Application.Invites;

internal sealed class InvitationService(IInvitationRepository invitations, IMemoryCache cache) : IInvitationService
{
    public async ValueTask<int> GetBonusAsync(PlayerId playerId, CancellationToken cancellationToken)
    {
        var key = CacheKeyPrefixes.Invitation + playerId;
        if (!cache.TryGetValue(key, out int bonus))
        {
            bonus = (await invitations.GetPendingInviteAsync(playerId, cancellationToken))?.BonusAmount ?? 0;
            cache.Set(key, bonus, TimeSpan.FromHours(1));
        }

        return bonus;
    }

    public void ClearPlayer(PlayerId playerId) => cache.Remove(CacheKeyPrefixes.Invitation + playerId);
}
