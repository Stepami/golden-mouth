using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GetWorldPosts;

public sealed record WorldPostsQuery(
    WorldId WorldId,
    int PageNumber,
    int PageSize,
    bool Feed = false)
    : IQuery<WorldPostsPage>;
