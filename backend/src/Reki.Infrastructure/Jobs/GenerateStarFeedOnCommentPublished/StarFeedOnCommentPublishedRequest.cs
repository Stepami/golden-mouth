using System.Text.Json.Serialization;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedOnCommentPublished;

public sealed record StarFeedOnCommentPublishedRequest(
    PlayerId PlayerId,
    WorldId WorldId,
    PostId PostId,
    [property: JsonRequired] long CreatedAtUnixMilliseconds,
    PostId SourcePostId,
    CommentId SourceCommentId);
