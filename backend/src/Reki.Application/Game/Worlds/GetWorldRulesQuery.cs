using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetWorldRulesQuery(long TelegramUserId, string WorldId) : IRequest<WorldRules>;

