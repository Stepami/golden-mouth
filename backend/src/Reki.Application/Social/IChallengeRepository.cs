using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public interface IChallengeRepository
{
    ValueTask<GameChallenge?> GetActiveChallengeAsync(
        PlayerId playerId,
        CancellationToken cancellationToken);

    ValueTask<GameChallenge?> GetPendingChallengeAsync(
        PlayerId firstPlayerId,
        PlayerId secondPlayerId,
        CancellationToken cancellationToken);

    ValueTask<bool> TryInsertChallengeAsync(
        GameChallenge challenge,
        CancellationToken cancellationToken);

    ValueTask<bool> TryReplaceChallengeAsync(
        GameChallenge challenge,
        int expectedVersion,
        CancellationToken cancellationToken);
}
