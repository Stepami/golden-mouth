using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Worlds;

public sealed record UpdateWorldRulesRequest
{
    [Required]
    [MaxLength(120)]
    public required string Title { get; init; }

    public required IReadOnlyList<string> Presets { get; init; }

    [Required]
    [StringLength(2000)]
    public required string Description { get; init; }

    public required IReadOnlyList<WorldRuleResponse> Rules { get; init; }
}

