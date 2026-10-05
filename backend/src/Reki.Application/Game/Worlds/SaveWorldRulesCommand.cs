using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record SaveWorldRulesCommand(long TelegramUserId, WorldRules Rules) : IRequest;

