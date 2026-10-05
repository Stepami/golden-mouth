namespace Reki.Domain.Player;

public sealed record PlayerProfile(
    string Id,
    long TelegramUserId,
    string BirthGroup,
    string Name,
    string Handle,
    string Role,
    string Avatar,
    string Address,
    string World,
    long Subscribers,
    int PostsUsed,
    int DailyBonus,
    DateTimeOffset PostsPeriodStartedAtUtc,
    bool PlusInterestClaimed,
    int StreakDays,
    PlayerSettings Settings,
    IReadOnlyDictionary<string, SkillProgress> Skills,
    DateTimeOffset? PausedAtUtc,
    DateTimeOffset? HateDisabledUntilUtc,
    int Version)
{
    public int GetPostsCap(int freePerDay, int plusPerDay)
    {
        int baseLimit = Settings.Plus ? plusPerDay : freePerDay;
        return baseLimit + DailyBonus;
    }

    public PlayerProfile StartNewPeriod(DateTimeOffset periodStartedAtUtc)
    {
        return this with
        {
            PostsUsed = 0,
            DailyBonus = 0,
            PostsPeriodStartedAtUtc = periodStartedAtUtc,
            Version = Version + 1,
        };
    }

    public PlayerProfile ConsumePost(long subscriberDelta)
    {
        return this with
        {
            Subscribers = Math.Max(0, Subscribers + subscriberDelta),
            PostsUsed = PostsUsed + 1,
            Version = Version + 1,
        };
    }

    public PlayerProfile ClaimPlusInterestBonus(int bonus)
    {
        if (PlusInterestClaimed)
        {
            return this;
        }

        return this with
        {
            DailyBonus = DailyBonus + bonus,
            PlusInterestClaimed = true,
            Version = Version + 1,
        };
    }

    public PlayerProfile StartCrisisPause(DateTimeOffset now, TimeSpan hateCooldown)
    {
        return this with
        {
            PausedAtUtc = now,
            HateDisabledUntilUtc = now.Add(hateCooldown),
            Version = Version + 1,
        };
    }
}