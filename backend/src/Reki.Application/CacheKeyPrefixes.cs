namespace Reki.Application;

public static class CacheKeyPrefixes
{
    public const string WorldTemplates = "world-templates";
    public const string PlayerWorlds = "player-worlds:";
    public const string PlayerStreak = "player-streak:";
    public const string UpdateLastSeenAt = nameof(Player.UpdateLastSeenAtCommand.UpdateLastSeenAt);
}
