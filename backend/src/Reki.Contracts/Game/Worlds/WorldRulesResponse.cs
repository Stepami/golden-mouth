using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record WorldRulesResponse(
    string Title,
    IReadOnlyList<string> Presets,
    string Description,
    IReadOnlyList<WorldRuleResponse> Rules);

