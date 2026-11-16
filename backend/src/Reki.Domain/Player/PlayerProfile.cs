using System.Collections.ObjectModel;

namespace Reki.Domain.Player;

public sealed class PlayerProfile
{
    private readonly List<ExternalUserId> _externalUserIds;
    private readonly List<PlayerNotificationChannel> _notificationChannels;
    private readonly Dictionary<SkillId, SkillProgress> _skills;

#pragma warning disable AV1553 // Older player documents do not contain notification channels.
    public PlayerProfile(
        PlayerId id,
        IReadOnlyList<ExternalUserId> externalUserIds,
        Address address,
        PlayerWallet wallet,
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        int birthYear,
        PlayerSettings settings,
        bool hitOwnWorldMilestone = false,
        DateTimeOffset lastSeenAt = default,
        bool plusInterestClaimed = false,
        int streakDays = 0,
        DateTimeOffset? pausedAtUtc = null,
        DateTimeOffset? hateDisabledUntilUtc = null,
        int version = 0,
        bool plusNotifyRequested = false,
        IReadOnlyList<PlayerNotificationChannel>? notificationChannels = null)
    {
        Id = id;
        _externalUserIds = [.. externalUserIds];
        ExternalUserIds = _externalUserIds.AsReadOnly();
        Address = address;
        Wallet = new PlayerWallet(wallet.Energy, wallet.Crystals, wallet.EnergyUpdatedAtUtc);
        _skills = new Dictionary<SkillId, SkillProgress>(skills);
        Skills = new ReadOnlyDictionary<SkillId, SkillProgress>(_skills);
        BirthYear = birthYear;
        Settings = settings;
        HitOwnWorldMilestone = hitOwnWorldMilestone;
        LastSeenAt = lastSeenAt;
        PlusInterestClaimed = plusInterestClaimed;
        StreakDays = streakDays;
        PausedAtUtc = pausedAtUtc;
        HateDisabledUntilUtc = hateDisabledUntilUtc;
        Version = version;
        PlusNotifyRequested = plusNotifyRequested;
        _notificationChannels = notificationChannels is null ? [] : [.. notificationChannels];
        NotificationChannels = _notificationChannels.AsReadOnly();
    }
#pragma warning restore AV1553

    public PlayerId Id { get; private set; }

    public IReadOnlyList<ExternalUserId> ExternalUserIds { get; }

    public Address Address { get; private set; }

    public bool HitOwnWorldMilestone { get; private set; }

    public PlayerWallet Wallet { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public bool PlusInterestClaimed { get; private set; }

    public int StreakDays { get; private set; }

    public IReadOnlyDictionary<SkillId, SkillProgress> Skills { get; }

    public DateTimeOffset? PausedAtUtc { get; private set; }

    public DateTimeOffset? HateDisabledUntilUtc { get; private set; }

    public int Version { get; private set; }

    public bool PlusNotifyRequested { get; private set; }

    public int BirthYear { get; private set; }

    public PlayerSettings Settings { get; private set; }

    public IReadOnlyList<PlayerNotificationChannel> NotificationChannels { get; }

    public PlayerProfile AddNotificationChannel(PlayerNotificationChannel channel)
    {
        if (SetNotificationChannel(channel))
        {
            Version++;
        }

        return this;
    }

    public PlayerProfile AddExternalUserIds(IReadOnlyList<ExternalUserId> ids)
    {
        var changed = false;
        for (var index = 0; index < ids.Count; index++)
        {
            if (!_externalUserIds.Contains(ids[index]))
            {
                _externalUserIds.Add(ids[index]);
                changed = true;
            }

            if (ids[index].TryGetTelegramId(out var chatId))
            {
                changed |= SetNotificationChannel(PlayerNotificationChannel.Telegram(chatId.Value));
            }
        }

        if (changed)
        {
            Version++;
        }

        return this;
    }

    public PlayerProfile ClaimPlusInterestBonus(int bonus)
    {
        if (!PlusInterestClaimed)
        {
            Wallet.Replenish(bonus);
            PlusInterestClaimed = true;
            Version++;
        }

        return this;
    }

    public PlayerProfile RequestPlusNotification()
    {
        if (!PlusNotifyRequested)
        {
            PlusNotifyRequested = true;
            Version++;
        }

        return this;
    }

    public PlayerProfile StartCrisisPause(DateTimeOffset now, TimeSpan hateCooldown)
    {
        var disabledUntil = now.Add(hateCooldown);
        if (PausedAtUtc != now || HateDisabledUntilUtc != disabledUntil)
        {
            PausedAtUtc = now;
            HateDisabledUntilUtc = disabledUntil;
            Version++;
        }

        return this;
    }

    public PlayerProfile AwardReferral()
    {
        Wallet.Replenish(3);
        HitOwnWorldMilestone = true;
        Version++;
        return this;
    }

    public PlayerProfile SpendPost(
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        int cost,
        DateTimeOffset now,
        bool hitOwnMilestone)
    {
        Wallet.Spend(cost, now);
        if (!ReferenceEquals(skills, Skills))
        {
            _skills.Clear();
            foreach (var (id, skill) in skills)
            {
                _skills.Add(id, skill);
            }
        }

        HitOwnWorldMilestone = hitOwnMilestone;
        Version++;
        return this;
    }

    public PlayerProfile SpendEnergy(int cost, DateTimeOffset now)
    {
        Wallet.Spend(cost, now);
        Version++;
        return this;
    }

    public void RestoreEnergy(DateTimeOffset now)
    {
        if (Wallet.Restore(now))
        {
            Version++;
        }
    }

    public void ExchangeEnergy(DateTimeOffset now)
    {
        Wallet.Exchange(now);
        Version++;
    }

    public void ReplenishCrystals(int amount)
    {
        Wallet.Replenish(amount);
        if (amount != 0)
        {
            Version++;
        }
    }

    public void UpdateLastSeenAt(DateTimeOffset now)
    {
        if (LastSeenAt != now)
        {
            LastSeenAt = now;
            Version++;
        }
    }

    public void UpdateSettings(PlayerSettings settings, Address address, int crystals)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(crystals);
        if (Settings != settings || Address != address || crystals != 0)
        {
            Settings = settings;
            Address = address;
            Wallet.Replenish(crystals);
            Version++;
        }
    }

    private bool SetNotificationChannel(PlayerNotificationChannel channel)
    {
        PlayerNotificationChannel? existing = null;
        var count = 0;
        foreach (var item in _notificationChannels)
        {
            if (item.Type == channel.Type)
            {
                existing = item;
                count++;
            }
        }

        if (count == 1 && existing is not null && existing.Properties.Count == channel.Properties.Count &&
            existing.Properties.All(item => channel.Properties.TryGetValue(item.Key, out var value) && value == item.Value))
        {
            return false;
        }

        _notificationChannels.RemoveAll(item => item.Type == channel.Type);
        _notificationChannels.Add(channel);
        return true;
    }
}
