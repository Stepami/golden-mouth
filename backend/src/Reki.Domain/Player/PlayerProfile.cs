namespace Reki.Domain.Player;

public sealed record PlayerProfile
{
    public required PlayerId Id { get; init; }

    public required IReadOnlyList<ExternalUserId> ExternalUserIds { get; init; }

    public required Address Address { get; init; }

    public bool HitOwnWorldMilestone { get; init; }

    public required PlayerWallet Wallet { get; init; }

    public DateTimeOffset LastSeenAt { get; init; }

    public bool PlusInterestClaimed { get; init; }

    public int StreakDays { get; init; }

    public required IReadOnlyDictionary<SkillId, SkillProgress> Skills { get; init; }

    public DateTimeOffset? PausedAtUtc { get; init; }

    public DateTimeOffset? HateDisabledUntilUtc { get; init; }

    public int Version { get; init; }

    public bool PlusNotifyRequested { get; init; }

    public required int BirthYear { get; init; }

    public required PlayerSettings Settings { get; init; }

    public IReadOnlyList<PlayerNotificationChannel> NotificationChannels { get; init; } = [];

    public PlayerProfile AddNotificationChannel(PlayerNotificationChannel channel) =>
        this with
        {
            NotificationChannels = [.. NotificationChannels.Where(item => item.Type != channel.Type), channel],
        };

    public PlayerProfile AddExternalUserIds(IReadOnlyList<ExternalUserId> ids)
    {
        var updated = this with { ExternalUserIds = ExternalUserIds.Concat(ids).Distinct().ToArray() };
        for (var index = 0; index < ids.Count; index++)
        {
            if (ids[index].TryGetTelegramId(out var chatId))
            {
                updated = updated.AddNotificationChannel(PlayerNotificationChannel.Telegram(chatId.Value));
            }
        }

        return updated;
    }

    public PlayerProfile ClaimPlusInterestBonus(int bonus)
    {
        if (PlusInterestClaimed)
        {
            return this;
        }

        return this with
        {
            Wallet = Wallet.Replenish(bonus),
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

    public PlayerProfile AwardReferral() => this with
    {
        Wallet = Wallet with { Crystals = Wallet.Crystals + 3 },
        HitOwnWorldMilestone = true,
        Version = Version + 1,
    };

    public PlayerProfile WithWallet(PlayerWallet wallet) => this with { Wallet = wallet, Version = Version + 1 };

    public PlayerProfile SpendPost(
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        int cost,
        DateTimeOffset now,
        bool hitOwnMilestone) =>
        SpendEnergy(cost, now) with
        {
            Skills = skills,
            HitOwnWorldMilestone = hitOwnMilestone,
        };

    public PlayerProfile SpendEnergy(int cost, DateTimeOffset now)
    {
        return this with { Wallet = Wallet.Spend(cost, now), Version = Version + 1 };
    }
}
