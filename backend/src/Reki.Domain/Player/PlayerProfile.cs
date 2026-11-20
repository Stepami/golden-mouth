using System.Collections.ObjectModel;
using Reki.Domain.Player.Challenges;
using Reki.Domain.Worlds;

namespace Reki.Domain.Player;

public sealed class PlayerProfile
{
    private readonly List<ExternalUserId> _externalUserIds;
    private readonly List<PlayerNotificationChannel> _notificationChannels;
    private readonly Dictionary<SkillId, SkillProgress> _skills;

    public PlayerProfile(
        PlayerId id,
        IReadOnlyList<ExternalUserId> externalUserIds,
        Address address,
        PlayerWallet wallet,
        IReadOnlyDictionary<SkillId, SkillProgress> skills,
        int birthYear,
        PlayerSettings settings,
        IReadOnlyList<PlayerNotificationChannel> notificationChannels,
        bool hitOwnWorldMilestone = false,
        DateTimeOffset lastSeenAt = default,
        bool plusInterestClaimed = false,
        DateTimeOffset? pausedAtUtc = null,
        DateTimeOffset? hateDisabledUntilUtc = null,
        int version = 0,
        bool plusNotifyRequested = false,
        PlayerChallenge? dailyChallenge = null)
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
        PausedAtUtc = pausedAtUtc;
        HateDisabledUntilUtc = hateDisabledUntilUtc;
        Version = version;
        PlusNotifyRequested = plusNotifyRequested;
        DailyChallenge = dailyChallenge ?? PlayerChallenge.Empty;
        _notificationChannels = [.. notificationChannels];
        NotificationChannels = _notificationChannels.AsReadOnly();
    }

    public PlayerId Id { get; private set; }

    public IReadOnlyList<ExternalUserId> ExternalUserIds { get; }

    public Address Address { get; private set; }

    public bool HitOwnWorldMilestone { get; private set; }

    public PlayerWallet Wallet { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public bool PlusInterestClaimed { get; private set; }

    public IReadOnlyDictionary<SkillId, SkillProgress> Skills { get; }

    public DateTimeOffset? PausedAtUtc { get; private set; }

    public DateTimeOffset? HateDisabledUntilUtc { get; private set; }

    public int Version { get; private set; }

    public bool PlusNotifyRequested { get; private set; }

    public int BirthYear { get; private set; }

    public PlayerSettings Settings { get; private set; }

    public IReadOnlyList<PlayerNotificationChannel> NotificationChannels { get; }

    public PlayerChallenge DailyChallenge { get; private set; }

    public void RefreshDailyChallenge(
        DateOnly day,
        IReadOnlyCollection<World> worlds,
        IReadOnlyDictionary<DailyTaskType, int[]> targets)
    {
        if (DailyChallenge.Day == day)
        {
            return;
        }

        DailyChallenge = PlayerChallenge.New(
            day,
            worlds,
            targets.ToDictionary(
                target => target.Key,
                target => Random.Shared.NextInt64(target.Value[0], target.Value[1] + 1)));
        Version++;
    }

    public void AddDailyChallengeWorld(
        World world,
        IReadOnlyDictionary<DailyTaskType, int[]> targets)
    {
        if (DailyChallenge.Day == DateOnly.MinValue || DailyChallenge.Values.ContainsKey(world.Id))
        {
            return;
        }

        DailyChallenge.AddWorld(
            world,
            targets.ToDictionary(
                target => target.Key,
                target => Random.Shared.NextInt64(target.Value[0], target.Value[1] + 1)));

        Version++;
    }

    public void RemoveDailyChallengeWorld(WorldId worldId)
    {
        if (DailyChallenge.RemoveWorld(worldId))
        {
            Version++;
        }
    }

    public void ApplyDailyChallengeProgress(
        WorldId worldId,
        IReadOnlyDictionary<DailyTaskType, long> changes,
        StarId? starId = null)
    {
        if (DailyChallenge.ApplyProgress(worldId, changes, starId))
        {
            Version++;
        }
    }

    public PlayerProfile AddNotificationChannel(PlayerNotificationChannel channel)
    {
        switch (channel.Type)
        {
            case NotificationChannelType.WebPush:
            {
                if (!_notificationChannels.Any(channel.IsSameWebPush))
                {
                    _notificationChannels.Add(channel);
                    Version++;
                }

                break;
            }

            case NotificationChannelType.Telegram:
            {
                _notificationChannels.RemoveAll(item => item.Type == NotificationChannelType.Telegram);
                _notificationChannels.Add(channel);
                Version++;

                break;
            }
        }

        return this;
    }

    public PlayerProfile AddExternalUserIds(IReadOnlyList<ExternalUserId> ids)
    {
        var previousVersion = Version;
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
                AddNotificationChannel(PlayerNotificationChannel.Telegram(chatId.Value));
            }
        }

        if (changed && Version == previousVersion)
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

    public PlayerProfile UpdateLastSeenAt(DateTimeOffset now)
    {
        if (LastSeenAt != now)
        {
            LastSeenAt = now;
            Version++;
        }

        return this;
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
}
