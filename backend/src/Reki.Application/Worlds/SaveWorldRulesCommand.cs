using Mediator;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public sealed record SaveWorldRulesCommand(
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRule> Rules) : IRequest;

