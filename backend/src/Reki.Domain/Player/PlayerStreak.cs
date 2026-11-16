namespace Reki.Domain.Player;

public sealed class PlayerStreak(DateTimeOffset startedAt, int count, bool isActive)
{
    public DateTimeOffset StartedAt { get; private set; } = startedAt;

    public int Count { get; private set; } = count;

    public bool IsActive { get; private set; } = isActive;

    public static PlayerStreak New(DateTimeOffset startedAt) => new(startedAt, 1, true);

    internal void Progress(DateTimeOffset lastSeenAt, DateTimeOffset now)
    {
        var days = (now.UtcDateTime.Date - lastSeenAt.UtcDateTime.Date).Days;
        if (days == 0)
        {
            return;
        }

        if (days == 1)
        {
            Count++;
        }
        else
        {
            IsActive = false;
        }
    }
}
