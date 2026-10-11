namespace Reki.Domain.Player;

public sealed record PlayerProfile
{
    public required PlayerId Id { get; init; }

    public required TelegramUserId TelegramUserId { get; init; }

    public required string BirthGroup { get; init; }

    public required string Address { get; init; }

    public int PostsUsed { get; init; }

    public int DailyBonus { get; init; }

    public DateTimeOffset PostsPeriodStartedAtUtc { get; init; }

    public bool PlusInterestClaimed { get; init; }

    public int StreakDays { get; init; }

    public required IReadOnlyDictionary<SkillId, SkillProgress> Skills { get; init; }

    public DateTimeOffset? PausedAtUtc { get; init; }

    public DateTimeOffset? HateDisabledUntilUtc { get; init; }

    public int Version { get; init; }

    public int ChallengeDailyBonus { get; init; }

    public DateTimeOffset? ChallengeBonusUntilUtc { get; init; }

    public bool PlusNotifyRequested { get; init; }

    public ReferralBenefits ReferralBenefits { get; init; } = ReferralBenefits.Empty;

    public string? AgeBand { get; init; }

    public required PlayerSettings Settings { get; init; }

    public int GetPostsCap(int freePerDay, int plusPerDay)
    {
        var baseLimit = Settings.Plus ? plusPerDay : freePerDay;
        var challengeBonus = ChallengeBonusUntilUtc > PostsPeriodStartedAtUtc
            ? ChallengeDailyBonus
            : 0;
        return baseLimit + DailyBonus + challengeBonus + ReferralBenefits.DailyPosts;
    }

    public PlayerProfile ApplyChallengeWinBonus(
        int dailyBonus,
        DateTimeOffset bonusUntilUtc)
    {
        return this with
        {
            ChallengeDailyBonus = dailyBonus,
            ChallengeBonusUntilUtc = bonusUntilUtc,
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

    public PlayerProfile RequestPlusNotification()
    {
        return PlusNotifyRequested
            ? this
            : this with { PlusNotifyRequested = true, Version = Version + 1 };
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

    public PlayerProfile SpendPost(IReadOnlyDictionary<SkillId, SkillProgress> skills)
    {
        return this with
        {
            PostsUsed = PostsUsed + 1,
            Skills = skills,
            Version = Version + 1,
        };
    }
}