using Reki.Application.Common.Exceptions;
using Reki.Domain.Player;

namespace Reki.Application.Common.Safety;

public sealed class PlayerSafetyGuard(
    IGameStore store,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan HateCooldown = TimeSpan.FromDays(7);

    public async ValueTask EnsureInputAllowedAsync(
        PlayerProfile player,
        IEnumerable<string> texts,
        CancellationToken cancellationToken)
    {
        var decision = texts
            .Select(ContentSafetyPolicy.CheckInput)
            .OrderByDescending(item => item)
            .FirstOrDefault();
        if (decision == SafetyDecision.Allowed)
        {
            return;
        }

        if (decision == SafetyDecision.Rejected)
        {
            throw new ContentRejectedException();
        }

        var now = timeProvider.GetUtcNow();
        var paused = player.StartCrisisPause(now, HateCooldown);
        var replaced = await store.TryReplacePlayerAsync(paused, player.Version, cancellationToken);
        if (!replaced)
        {
            throw new RequestConflictException("The player state changed while applying crisis safety.");
        }

        throw new CrisisDetectedException(player.BirthGroup == "14-17");
    }
}
