namespace Reki.Domain.Worlds;

public sealed record WorldRules(
    WorldRulesId Id,
    string PlayerId,
    string WorldId,
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRule> Rules);

