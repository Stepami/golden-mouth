using Mediator;
using Reki.Domain.Game.Worlds;

namespace Reki.Application.Game.Worlds;

public sealed record SaveWorldRulesCommand(long TelegramUserId, WorldRules Rules) : IRequest;

