using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public interface IStarFeedScheduler
{
    ValueTask ScheduleAsync(
        PlayerId playerId,
        WorldId worldId,
        StarFeedEvent sourceEvent,
        PostId? sourcePostId,
        CommentId? sourceCommentId);

    ValueTask ScheduleFromFeedAsync(PlayerId playerId, WorldId worldId);
}
