using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Comment;

public sealed record ScheduleFromCommentRequest(
    PlayerId PlayerId,
    WorldId WorldId,
    PostId SourcePostId,
    CommentId SourceCommentId);
