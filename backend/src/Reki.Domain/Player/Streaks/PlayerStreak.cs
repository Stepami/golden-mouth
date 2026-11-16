namespace Reki.Domain.Player.Streaks;

public sealed class PlayerStreak(
    StreakId id,
    PlayerId playerId,
    DateTimeOffset startedAt,
    int count,
    bool isActive)
{
    public StreakId Id { get; private set; } = id;

    public PlayerId PlayerId { get; private set; } = playerId;

    public DateTimeOffset StartedAt { get; private set; } = startedAt;

    public int Count { get; private set; } = count;

    public bool IsActive { get; private set; } = isActive;

    public static PlayerStreak New(PlayerId playerId, DateTimeOffset startedAt) =>
        new(StreakId.New(startedAt), playerId, startedAt, 1, true);

    public bool Progress(DateTimeOffset now)
    {
        var lastStreakDay = StartedAt.UtcDateTime.Date.AddDays(Count - 1);
        var days = (now.UtcDateTime.Date - lastStreakDay).Days;
        if (!IsActive || days <= 0)
        {
            return false;
        }

        if (days == 1)
        {
            Count++;
        }
        else
        {
            IsActive = false;
        }

        return true;
    }
}
