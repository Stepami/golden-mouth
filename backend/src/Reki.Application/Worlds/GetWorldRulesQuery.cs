using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record GetWorldRulesQuery(long TelegramUserId, string WorldId) : IRequest<WorldRules>;

