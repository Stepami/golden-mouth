using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public sealed record GetPostQuery(
    PostId PostId,
    PlayerId PlayerId,
    WorldId WorldId)
    : IRequest<GamePost>;