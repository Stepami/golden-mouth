using Mediator;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GetWorldPosts;

public sealed record WorldPostsQuery(
    WorldId WorldId,
    AuthorId[] AuthorIds,
    PostId? Cursor,
    int PageSize,
    AuthorId? LikedBy = null)
    : IQuery<WorldPostsPage>;
