using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record GetWorldRulesQuery(TelegramUserId TelegramUserId, WorldId WorldId) : IRequest<WorldRules>;

