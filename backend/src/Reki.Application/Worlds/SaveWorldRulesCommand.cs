using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record SaveWorldRulesCommand(
    TelegramUserId TelegramUserId,
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRule> Rules) : IRequest;

