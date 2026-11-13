namespace Reki.Domain.Player;

public static class AchievementCatalog
{
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

    public static IReadOnlyList<AchievementProgress> Create()
    {
        return
        [
            new(StarStory, "💬", "История звезды", false),
            new(FirstViral, "🌊", "Первый залёт", false),
            new(HateSurvived, "🌩", "Хейт пережит", false),
            new(Collaboration, "🎬", "Коллаб", false),
            new(TenThousand, "⭐", "10k", false),
            new(SkillMastery, "🎓", "Мастерство", false),
            new(ProfileFrame, "🖼", "Рамка профиля", false),
            new(Secret, "❔", "Тайная", false),
            new(Duet, "🤝", "Дуэт", false),
            new(Challenge, "⚔️", "Заруба", false),
            new(Rematch, "🔁", "Реванш", false),
        ];
    }
}
