using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public interface IStarFeedScheduler
{
    ValueTask ScheduleAsync(
        PlayerId playerId,
        WorldId worldId,
        string trigger,
        string triggerId,
        CancellationToken cancellationToken);
}