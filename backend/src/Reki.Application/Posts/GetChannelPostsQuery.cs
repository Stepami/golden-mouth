using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record GetChannelPostsQuery(WorldId WorldId, PlayerId? PlayerId = null, int? Limit = null)
    : IRequest<IReadOnlyList<GamePost>>;