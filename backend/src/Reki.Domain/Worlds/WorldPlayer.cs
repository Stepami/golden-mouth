using Reki.Domain.Player;

namespace Reki.Domain.Worlds;

public sealed class WorldPlayer
{
    private readonly List<AchievementProgress> _achievements;

    public WorldPlayer(
        PlayerId playerId,
        string voice,
        WorldDrama drama,
        string name,
        string role,
        string avatar,
        long subscribers,
        IReadOnlyList<AchievementProgress> achievements)
    {
        PlayerId = playerId;
        Voice = voice;
        Drama = drama;
        Name = name;
        Role = role;
        Avatar = avatar;
        Subscribers = subscribers;
        _achievements = [.. achievements];
        Achievements = _achievements.AsReadOnly();
    }

    public PlayerId PlayerId { get; private set; }

    public string Voice { get; private set; }

    public WorldDrama Drama { get; private set; }

    public string Name { get; private set; }

    public string Role { get; private set; }

    public string Avatar { get; private set; }

    public long Subscribers { get; private set; }

    public IReadOnlyList<AchievementProgress> Achievements { get; }

    internal bool SetSubscribers(long subscribers)
    {
        if (Subscribers == subscribers)
        {
            return false;
        }

        Subscribers = subscribers;
        return true;
    }

    internal bool UnlockAchievement(string id)
    {
        var changed = false;
        for (var index = 0; index < _achievements.Count; index++)
        {
            var achievement = _achievements[index];
            if (achievement.Id == id && !achievement.IsOpen)
            {
                _achievements[index] = achievement with { IsOpen = true };
                changed = true;
            }
        }

        return changed;
    }

    internal bool UpdateDefinition(WorldPlayer definition)
    {
        var achievementsChanged = _achievements.Count != definition.Achievements.Count;
        for (var index = 0; !achievementsChanged && index < _achievements.Count; index++)
        {
            var current = _achievements[index];
            var incoming = definition.Achievements[index];
            achievementsChanged = current.Id != incoming.Id || current.Emoji != incoming.Emoji ||
                current.Title != incoming.Title || current.IsOpen != IsAchievementOpen(incoming.Id);
        }

        var changed = Name != definition.Name || Avatar != definition.Avatar || Role != definition.Role ||
            Voice != definition.Voice || Drama != definition.Drama || achievementsChanged;
        if (!changed)
        {
            return false;
        }

        Name = definition.Name;
        Avatar = definition.Avatar;
        Role = definition.Role;
        Voice = definition.Voice;
        Drama = definition.Drama;
        if (achievementsChanged)
        {
            var achievements = new List<AchievementProgress>(definition.Achievements.Count);
            foreach (var achievement in definition.Achievements)
            {
                var isOpen = IsAchievementOpen(achievement.Id);
                achievements.Add(achievement.IsOpen == isOpen ? achievement : achievement with { IsOpen = isOpen });
            }

            _achievements.Clear();
            _achievements.AddRange(achievements);
        }

        return true;
    }

    private bool IsAchievementOpen(string id)
    {
        foreach (var achievement in _achievements)
        {
            if (achievement.Id == id)
            {
                return achievement.IsOpen;
            }
        }

        return false;
    }
}
