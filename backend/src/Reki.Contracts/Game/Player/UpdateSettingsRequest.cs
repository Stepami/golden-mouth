using System.ComponentModel.DataAnnotations;

namespace Reki.Contracts.Game.Player;

public sealed record UpdateSettingsRequest
{
    public Drama? Drama { get; init; }

    [MaxLength(64)]
    public string? Notifications { get; init; }

    [MaxLength(64)]
    public string? Quiet { get; init; }

    [MaxLength(64)]
    public string? Voice { get; init; }

    public bool? Plus { get; init; }
}

