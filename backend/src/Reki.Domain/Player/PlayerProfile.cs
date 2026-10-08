using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Domain.Player;

public sealed record PlayerProfile(
    PlayerId Id,
    TelegramUserId TelegramUserId,
    string BirthGroup,
    string Name,
    string Handle,
    string Role,
    string Avatar,
    string Address,
    WorldId World,
    long Subscribers,
    int PostsUsed,
    int DailyBonus,
    DateTimeOffset PostsPeriodStartedAtUtc,
    bool PlusInterestClaimed,
    int StreakDays,
    PlayerSettings Settings,
    IReadOnlyDictionary<SkillId, SkillProgress> Skills,
    DateTimeOffset? PausedAtUtc,
    DateTimeOffset? HateDisabledUntilUtc,
    int Version,
    IReadOnlyList<DailyWant> Wants,
    IReadOnlyList<AchievementProgress> Achievements,
    IReadOnlyList<StarRelationProgress> StarRelations,
    DateTimeOffset? LastPostAtUtc,
    int RestedPostsRemaining,
    int TotalPostsPublished,
    int PostsSinceViral,
    int ConsecutiveFlops,
    int ChallengeDailyBonus,
    DateTimeOffset? ChallengeBonusUntilUtc)
{
    public bool PlusNotifyRequested { get; init; }

    public PlayerId? InvitedBy { get; init; }

    public DateTimeOffset? InvitedAtUtc { get; init; }

    public PlayerProfile EnsureDefaults()
    {
        // Mongo can hydrate null for fields absent from legacy documents despite the domain contract.

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        IReadOnlyList<DailyWant> wants = Wants ?? DailyWantCatalog.Create();

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        IReadOnlyList<AchievementProgress> achievements = Achievements ?? [];
        if (!achievements.Any(item => item.WorldId == World))
        {
            achievements = [.. achievements, .. AchievementCatalog.Create(World)];
        }

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        IReadOnlyList<StarRelationProgress> relations = StarRelations ?? [];
        return ReferenceEquals(wants, Wants) &&
            ReferenceEquals(achievements, Achievements) &&
            ReferenceEquals(relations, StarRelations)
            ? this
            : this with
            {
                Wants = wants,
                Achievements = achievements,
                StarRelations = relations,
            };
    }

    public int GetPostsCap(int freePerDay, int plusPerDay)
    {
        int baseLimit = Settings.Plus ? plusPerDay : freePerDay;
        int challengeBonus = ChallengeBonusUntilUtc > PostsPeriodStartedAtUtc
            ? ChallengeDailyBonus
            : 0;
        return baseLimit + DailyBonus + challengeBonus;
    }

    public PlayerProfile StartNewPeriod(DateTimeOffset periodStartedAtUtc, int restedPosts)
    {
        return this with
        {
            PostsUsed = 0,
            DailyBonus = 0,
            PostsPeriodStartedAtUtc = periodStartedAtUtc,
            RestedPostsRemaining = restedPosts,
            Wants = Wants.Select(want => want with { Done = false }).ToArray(),
            StarRelations = StarRelations
                .Select(relation => relation with
                {
                    GainedInPeriod = 0,
                    PeriodStartedAtUtc = periodStartedAtUtc,
                })
                .ToArray(),
            Version = Version + 1,
        };
    }

    public PlayerProfile PublishPost(
        long subscriberDelta,
        bool viral,
        bool flop,
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        string? completedWantId,
        StarId starId,
        DateTimeOffset now,
        int relationMinimum,
        int relationMaximum,
        int relationMaxGainPerDay,
        int starCardAt)
    {
        bool completedWant = !string.IsNullOrWhiteSpace(completedWantId) &&
            Wants.Any(want => want.Id == completedWantId && !want.Done);
        IReadOnlyList<DailyWant> wants = completedWant
            ? Wants.Select(want => want.Id == completedWantId ? want with { Done = true } : want).ToArray()
            : Wants;
        IReadOnlyList<StarRelationProgress> relations = completedWant
            ? GainStarRelation(
                starId,
                relationMinimum,
                relationMaximum,
                relationMaxGainPerDay)
            : StarRelations;
        long subscribers = Math.Max(0, Subscribers + subscriberDelta);
        IReadOnlyList<AchievementProgress> achievements = Achievements;
        if (viral)
        {
            achievements = Unlock(achievements, World, AchievementCatalog.FirstViral);
        }

        if (subscribers >= 10_000)
        {
            achievements = Unlock(achievements, World, AchievementCatalog.TenThousand);
        }

        if (skills.Values.Any(skill => skill.Level >= 3))
        {
            achievements = Unlock(achievements, World, AchievementCatalog.SkillMastery);
        }

        if (relations.Any(relation => relation.StarId == starId && relation.Value >= starCardAt))
        {
            achievements = Unlock(achievements, World, AchievementCatalog.StarCard);
        }

        return this with
        {
            Subscribers = subscribers,
            PostsUsed = PostsUsed + 1,
            Skills = skills,
            Wants = wants,
            Achievements = achievements,
            StarRelations = relations,
            LastPostAtUtc = now,
            RestedPostsRemaining = Math.Max(0, RestedPostsRemaining - 1),
            TotalPostsPublished = TotalPostsPublished + 1,
            PostsSinceViral = viral ? 0 : PostsSinceViral + 1,
            ConsecutiveFlops = flop ? ConsecutiveFlops + 1 : 0,
            Version = Version + 1,
        };
    }

    public PlayerProfile UnlockAchievement(string achievementId)
    {
        IReadOnlyList<AchievementProgress> updated = Unlock(Achievements, World, achievementId);
        return ReferenceEquals(updated, Achievements)
            ? this
            : this with { Achievements = updated, Version = Version + 1 };
    }

    public PlayerProfile AwardThreadBonus(long bonus)
    {
        return this with
        {
            Subscribers = Math.Max(0, Subscribers + bonus),
            Version = Version + 1,
        };
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

    public PlayerProfile StartCrisisPause(DateTimeOffset now, TimeSpan hateCooldown)
    {
        return this with
        {
            PausedAtUtc = now,
            HateDisabledUntilUtc = now.Add(hateCooldown),
            Version = Version + 1,
        };
    }

    private IReadOnlyList<StarRelationProgress> GainStarRelation(
        StarId starId,
        int minimum,
        int maximum,
        int maxGainPerDay)
    {
        StarRelationProgress? current = StarRelations.FirstOrDefault(item => item.StarId == starId);
        current ??= new StarRelationProgress(starId, 0, 0, PostsPeriodStartedAtUtc);
        int gained = current.PeriodStartedAtUtc == PostsPeriodStartedAtUtc
            ? current.GainedInPeriod
            : 0;
        int available = Math.Max(0, maxGainPerDay - gained);
        int increase = Math.Min(1, available);
        var updated = current with
        {
            Value = Math.Clamp(current.Value + increase, minimum, maximum),
            GainedInPeriod = gained + increase,
            PeriodStartedAtUtc = PostsPeriodStartedAtUtc,
        };
        return
        [
            .. StarRelations.Where(item => item.StarId != starId),
            updated,
        ];
    }

    private static IReadOnlyList<AchievementProgress> Unlock(
        IReadOnlyList<AchievementProgress> achievements,
        WorldId worldId,
        string achievementId)
    {
        AchievementProgress? target = achievements.FirstOrDefault(item =>
            item.WorldId == worldId && item.Id == achievementId);
        if (target is null || target.IsOpen)
        {
            return achievements;
        }

        return achievements
            .Select(item => item == target ? item with { IsOpen = true } : item)
            .ToArray();
    }
}
