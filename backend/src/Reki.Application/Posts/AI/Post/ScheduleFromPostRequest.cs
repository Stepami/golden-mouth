using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Post;

public sealed record ScheduleFromPostRequest(
    PlayerId PlayerId,
    WorldId WorldId,
    PostId SourcePostId);
