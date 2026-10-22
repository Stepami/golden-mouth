using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record WorldPostsQuery(
    WorldId WorldId,
    int PageNumber,
    int PageSize,
    bool Feed = false)
    : IRequest<WorldPostsPage>;
