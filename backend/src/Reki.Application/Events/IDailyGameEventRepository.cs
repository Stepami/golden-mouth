using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Events;

public interface IDailyGameEventRepository
{
    ValueTask<DailyGameEvent?> GetTodayEventAsync(
        PlayerId playerId,
        DateTimeOffset dayStartedAtUtc,
        CancellationToken cancellationToken,
        WorldId worldId);

    ValueTask SaveEventAsync(DailyGameEvent gameEvent, CancellationToken cancellationToken);

    ValueTask DeleteEventsAsync(PlayerId playerId, CancellationToken cancellationToken);
}