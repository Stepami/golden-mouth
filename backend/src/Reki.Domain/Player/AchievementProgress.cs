using Reki.Domain.Worlds;

namespace Reki.Domain.Player;

public sealed record AchievementProgress(
    WorldId WorldId,
    string Id,
    string Emoji,
    string Title,
    bool IsOpen);
