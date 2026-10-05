using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record SaveWorldRulesCommand(long TelegramUserId, WorldRules Rules) : IRequest;

