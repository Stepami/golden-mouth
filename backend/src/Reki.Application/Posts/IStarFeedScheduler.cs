using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public interface IStarFeedScheduler
{
    ValueTask ScheduleFromPostAsync(PlayerId playerId, WorldId worldId, PostId sourcePostId);

    ValueTask ScheduleFromCommentAsync(PlayerId playerId, WorldId worldId, PostId sourcePostId, CommentId sourceCommentId);

    ValueTask ScheduleFromFeedAsync(PlayerId playerId, WorldId worldId);
}
