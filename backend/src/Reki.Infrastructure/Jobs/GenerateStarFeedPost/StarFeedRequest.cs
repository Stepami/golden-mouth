using System.Text.Json.Serialization;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Jobs.GenerateStarFeedPost;

public sealed record StarFeedRequest(
    PlayerId PlayerId,
    WorldId WorldId,
    PostId PostId,
    [property: JsonRequired] long CreatedAtUnixMilliseconds,
    StarFeedEvent SourceEvent = StarFeedEvent.FeedOpened,
    PostId? SourcePostId = null,
    CommentId? SourceCommentId = null);
