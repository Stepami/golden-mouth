using Reki.Domain.Worlds;

namespace Reki.Domain.Player;

public static class AchievementCatalog
{
    public const string StarCard = "star_card";
    public const string StarStory = "star_story";
    public const string FirstViral = "first_viral";
    public const string HateSurvived = "hate_survived";
    public const string Collaboration = "collaboration";
    public const string TenThousand = "ten_thousand";
    public const string SkillMastery = "skill_mastery";
    public const string ProfileFrame = "profile_frame";
    public const string Secret = "secret";
    public const string Duet = "duet";
    public const string Challenge = "challenge";
    public const string Rematch = "rematch";

    public static IReadOnlyList<AchievementProgress> Create(WorldId worldId)
    {
        return
        [
            new(worldId, StarCard, "👑", "Карточка звезды", false),
            new(worldId, StarStory, "💬", "История звезды", false),
            new(worldId, FirstViral, "🌊", "Первый залёт", false),
            new(worldId, HateSurvived, "🌩", "Хейт пережит", false),
            new(worldId, Collaboration, "🎬", "Коллаб", false),
            new(worldId, TenThousand, "⭐", "10k", false),
            new(worldId, SkillMastery, "🎓", "Мастерство", false),
            new(worldId, ProfileFrame, "🖼", "Рамка профиля", false),
            new(worldId, Secret, "❔", "Тайная", false),
            new(worldId, Duet, "🤝", "Дуэт", false),
            new(worldId, Challenge, "⚔️", "Заруба", false),
            new(worldId, Rematch, "🔁", "Реванш", false),
        ];
    }
}
