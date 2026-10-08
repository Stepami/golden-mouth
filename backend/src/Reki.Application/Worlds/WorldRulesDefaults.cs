using Reki.Application.Social;
using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public static class WorldRulesDefaults
{
    public const string SafetyRule = "Без тем 18+ и травли по внешности";

    public static WorldRules Create(PlayerId playerId, WorldId worldId)
    {
        string title = worldId.Value switch
        {
            "stage" => "Сцена",
            "academy" => "Академия",
            "it" => "Айти-тусовка",
            "stream" => "Стрим",
            _ => "Свой мир",
        };
        string description = worldId.Value switch
        {
            "stage" => "Айдол-агентство: трейни, дебют, камбэки и фансайты.",
            "academy" => "Школьная академия: клубы, соперники и фестиваль.",
            "it" => "Блогеры, разоблачения, коалиции и стартапы.",
            "stream" => "Стримеры, донаты, рейды и турниры.",
            _ => "Мир, который создаёт игрок.",
        };

        IReadOnlyList<WorldRule> rules = worldId.Value switch
        {
            "stage" => Lock(
                CreateStarRule(worldId),
                "Фанаты: фансайты BLESK и NOIR, любят камбэки и честность",
                "Тон: поддержка и честная критика поста; хейт только после серверного порога мира",
                "События: коллабы с брендами, слухи фансайтов, прослушивания и хейт-волны"),
            "academy" => Lock(
                CreateStarRule(worldId),
                "Фанаты: школьные клубы и чат параллели, ценят участие в жизни академии",
                "Тон: клубное соперничество, поддержка друзей и безопасная школьная драма",
                "События: фестиваль, слух о записке, приглашение в клуб и хейт в чате параллели"),
            "it" => Lock(
                CreateStarRule(worldId),
                "Фанаты: разработчики, основатели и читатели техноканалов, ценят конкретику",
                "Тон: профессиональная ирония, аргументы и критика только опубликованных идей",
                "События: реклама курса, слух о релизе, оффер от стартапа и разоблачение поста"),
            "stream" => Lock(
                CreateStarRule(worldId),
                "Фанаты: чаты стримеров, зрители турниров и участники рейдов",
                "Тон: живые реакции, игровой юмор и критика только контента",
                "События: спонсор железа, рейд, турнир и критика опубликованного игрового контента"),
            _ =>
            [
                new WorldRule(CreateStarRule(worldId), false),
                new WorldRule("Фанаты: аудитория и сообщества этого мира", false),
                new WorldRule("Тон: стиль реакций и общения в этом мире", false),
                new WorldRule("События: коллабы, слухи, шансы и хейт-волны", false),
                new WorldRule(SafetyRule, true),
            ],
        };

        return new WorldRules(
            WorldRulesId.New(),
            playerId,
            worldId,
            title,
            worldId.Value == "own" ? ["🏫 моя школа", "💻 IT-драма", "🏰 фэнтези-академия"] : [],
            description,
            rules);
    }

    private static string CreateStarRule(WorldId worldId) =>
        "Звёзды: " + string.Join("; ", StarCatalog.ForWorld(worldId).Select(star => $"{star.Name} ({star.Id}) — {star.Role}. {star.About}"));

    private static IReadOnlyList<WorldRule> Lock(params string[] rules)
    {
        return
        [
            .. rules.Select(rule => new WorldRule(rule, true)),
            new WorldRule(SafetyRule, true),
        ];
    }
}
