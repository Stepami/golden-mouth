using Reki.Domain.Player;

namespace Reki.Domain.Worlds;

public sealed record WorldPlayer
{
    public required PlayerId PlayerId { get; init; }

    public string Voice { get; init; } = "default";

    public WorldDrama Drama { get; init; } = WorldDrama.Mid;

    public string Name { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string Avatar { get; init; } = string.Empty;

    public long Subscribers { get; init; }

    public IReadOnlyList<AchievementProgress> Achievements { get; init; } = [];
}
