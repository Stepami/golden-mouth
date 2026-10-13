namespace Reki.Domain.Player;

public sealed record PlayerProfile
{
    public required PlayerId Id { get; init; }

    public required TelegramUserId TelegramUserId { get; init; }

    public required string BirthGroup { get; init; }

    public required string Address { get; init; }

    public int EnergyUsed { get; init; }

    public int Energy { get; init; }

    public DateTimeOffset PostsPeriodStartedAtUtc { get; init; }

    public bool PlusInterestClaimed { get; init; }

    public int StreakDays { get; init; }

    public required IReadOnlyDictionary<SkillId, SkillProgress> Skills { get; init; }

    public DateTimeOffset? PausedAtUtc { get; init; }

    public DateTimeOffset? HateDisabledUntilUtc { get; init; }

    public int Version { get; init; }

    public DateTimeOffset? ChallengeBonusUntilUtc { get; init; }

    public bool PlusNotifyRequested { get; init; }

    public string? AgeBand { get; init; }

    public required PlayerSettings Settings { get; init; }

    public PlayerProfile ApplyChallengeWinBonus(
        int dailyBonus,
        DateTimeOffset bonusUntilUtc,
        DateTimeOffset periodStartedAtUtc)
    {
        return this with
        {
            Energy = Energy + (ChallengeBonusUntilUtc > periodStartedAtUtc ? 0 : dailyBonus),
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
            Energy = Energy + bonus,
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

    public PlayerProfile StartNewPeriod(DateTimeOffset periodStartedAtUtc, int energy)
    {
        return this with
        {
            EnergyUsed = 0,
            Energy = energy,
            PostsPeriodStartedAtUtc = periodStartedAtUtc,
            Version = Version + 1,
        };
    }

    public PlayerProfile SpendPost(IReadOnlyDictionary<SkillId, SkillProgress> skills, int cost) =>
        SpendEnergy(cost) with { Skills = skills };

    public PlayerProfile SpendEnergy(int cost)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);
        if (Energy < cost)
        {
            throw new InvalidOperationException("Insufficient energy.");
        }

        return this with { Energy = Energy - cost, EnergyUsed = EnergyUsed + cost, Version = Version + 1 };
    }
}