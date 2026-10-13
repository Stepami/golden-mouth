using Mediator;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record GetChannelPostsQuery(
    WorldId WorldId,
    int? Limit = null,
    bool Feed = false)
    : IRequest<IReadOnlyList<GamePost>>;