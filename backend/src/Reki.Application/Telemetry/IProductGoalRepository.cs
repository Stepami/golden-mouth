using Reki.Domain.Player;

namespace Reki.Application.Telemetry;

public interface IProductGoalRepository
{
    ValueTask EnsurePlayerAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask EnrollReferralAsync(PlayerId playerId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<string>> RecordActivityAsync(
        PlayerId playerId,
        DateTimeOffset occurredAt,
        bool ordinaryPost,
        CancellationToken cancellationToken);

    ValueTask DeletePlayerAsync(PlayerId playerId, CancellationToken cancellationToken);
}
