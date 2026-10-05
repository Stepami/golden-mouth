using Mediator;
using Reki.Domain.Game.Worlds;

namespace Reki.Application.Game.Worlds;

public sealed record GetWorldRulesQuery(long TelegramUserId, string WorldId) : IRequest<WorldRules>;

