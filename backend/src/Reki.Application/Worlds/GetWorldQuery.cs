using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record GetWorldQuery(PlayerId PlayerId, WorldId WorldId) : IRequest<World>;