namespace Reki.Domain.Player.Streaks;

public sealed class PlayerStreak(
    StreakId id,
    PlayerId playerId,
    DateTimeOffset startedAt,
    int count,
    bool isActive)
{
    private static readonly int[] AwardDays = [3, 7, 10, 14, 21, 28, 30];

    public StreakId Id { get; private set; } = id;

    public PlayerId PlayerId { get; private set; } = playerId;

    public DateTimeOffset StartedAt { get; private set; } = startedAt;

    public int Count { get; private set; } = count;

    public bool IsActive { get; private set; } = isActive;

    public static PlayerStreak New(PlayerId playerId, DateTimeOffset startedAt) =>
        new(StreakId.New(startedAt), playerId, startedAt, 0, true);

    public bool ShouldAward() => IsActive && (AwardDays.Contains(Count) || (Count > AwardDays[^1] && Count % 5 == 0));

    public PlayerStreakProgress Progress(DateTimeOffset now)
    {
        if (Count == 0)
        {
            Count++;
            return PlayerStreakProgress.Started;
        }

        var lastStreakDay = StartedAt.UtcDateTime.Date.AddDays(Count - 1);
        var days = (now.UtcDateTime.Date - lastStreakDay).Days;
        if (days <= 0)
        {
            return PlayerStreakProgress.None;
        }

        if (days == 1)
        {
            Count++;
            return PlayerStreakProgress.Consecutive;
        }

        IsActive = false;
        return PlayerStreakProgress.Stopped;
    }
}
