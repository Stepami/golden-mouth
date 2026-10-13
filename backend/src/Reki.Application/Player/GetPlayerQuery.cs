using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Player;

public sealed record GetPlayerQuery : IRequest<PlayerProfile>;

