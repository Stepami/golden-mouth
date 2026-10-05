namespace Reki.Contracts.Game.Worlds;

public sealed record WorldRulesResponse(
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRuleResponse> Rules);

