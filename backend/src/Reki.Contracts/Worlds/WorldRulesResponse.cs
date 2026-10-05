namespace Reki.Contracts.Worlds;

public sealed record WorldRulesResponse(
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRuleResponse> Rules);

