namespace Reki.Domain.Player;

public sealed record AchievementProgress(
    string Id,
    string Emoji,
    string Title,
    bool IsOpen);