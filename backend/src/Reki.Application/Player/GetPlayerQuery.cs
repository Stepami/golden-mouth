using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record GetPlayerQuery(PlayerId? PlayerId = null) : IRequest<PlayerProfile>;

