namespace Reki.Domain.Game;

public sealed record WorldRules(
    string PlayerId,
    string WorldId,
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRule> Rules);

