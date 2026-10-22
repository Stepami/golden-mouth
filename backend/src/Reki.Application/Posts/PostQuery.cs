using Mediator;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record PostQuery(
    PostId PostId,
    WorldId WorldId)
    : IRequest<GamePost>;