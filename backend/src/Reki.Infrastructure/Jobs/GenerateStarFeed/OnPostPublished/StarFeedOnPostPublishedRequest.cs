using System.Text.Json.Serialization;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Infrastructure.Jobs.GenerateStarFeed.OnPostPublished;

public sealed record StarFeedOnPostPublishedRequest(
    PlayerId PlayerId,
    WorldId WorldId,
    PostId PostId,
    [property: JsonRequired] long CreatedAtUnixMilliseconds,
    PostId SourcePostId);
