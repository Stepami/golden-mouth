namespace Reki.Domain.Game.Worlds;

public sealed record WorldRules(
    string PlayerId,
    string WorldId,
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRule> Rules);

