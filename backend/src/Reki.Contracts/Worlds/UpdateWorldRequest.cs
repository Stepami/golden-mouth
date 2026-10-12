using System.ComponentModel.DataAnnotations;
using Reki.Contracts.Player;

namespace Reki.Contracts.Worlds;

public sealed record UpdateWorldRequest
{
    [MaxLength(120)]
    public string? Title { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    public IReadOnlyList<WorldRuleResponse>? Rules { get; init; }

    public Drama? Drama { get; init; }

    [MaxLength(200)]
    public string? Voice { get; init; }
}
