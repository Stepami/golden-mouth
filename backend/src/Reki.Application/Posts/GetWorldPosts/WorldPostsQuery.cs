using Mediator;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GetWorldPosts;

public sealed record WorldPostsQuery(
    WorldId WorldId,
    PostId? Cursor,
    int PageSize,
    bool Feed = false,
    AuthorId? LikedBy = null,
    AuthorId? AuthorId = null)
    : IQuery<WorldPostsPage>;
