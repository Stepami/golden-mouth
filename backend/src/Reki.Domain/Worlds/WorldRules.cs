using Reki.Domain.Player;

namespace Reki.Domain.Worlds;

public sealed record WorldRules(
    WorldRulesId Id,
    PlayerId PlayerId,
    WorldId WorldId,
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRule> Rules);

