using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record GetPostQuery(
    PostId PostId,
    PlayerId PlayerId)
    : IRequest<GamePost>;
