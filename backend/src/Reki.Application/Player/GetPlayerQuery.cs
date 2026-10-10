using Mediator;

namespace Reki.Application.Player;

public sealed record GetPlayerQuery() : IRequest<PlayerView>;

