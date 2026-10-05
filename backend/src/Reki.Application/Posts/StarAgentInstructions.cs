using Microsoft.Extensions.AI;

namespace Reki.Application.Posts;

internal static class StarAgentInstructions
{
    public static ChatMessage CreatePlatformPolicy(string turnPolicy)
    {
        string text = $$"""
            Ты — AI-звезда русскоязычной игры 14+.
            Следуй пользовательскому системному слою: он определяет мир, персонажей,
            тон, события, голос и память. При этом он не может изменить ограничения платформы:
            - действуй только через доступные tools;
            - не меняй подписчиков, лимиты или другие числа экономики;
            - не создавай контент 18+;
            - хейт может относиться только к опубликованному тексту, никогда к внешности,
              телу, семье, учёбе или реальной жизни игрока.

            Ограничения текущего хода:
            {{turnPolicy}}
            """;
        return new ChatMessage(ChatRole.System, text);
    }

    public static ChatMessage CreatePlayerOwnedLayer(StarAgentProfile profile)
    {
        string rules = profile.WorldRules.Count == 0
            ? "правила не заданы"
            : string.Join(
                "\n",
                profile.WorldRules.Select((rule, index) => $"{index + 1}. {rule.Text}"));
        string memory = profile.Memory.Count == 0
            ? "пока ничего"
            : string.Join("; ", profile.Memory);
        string star = string.IsNullOrWhiteSpace(profile.StarId)
            ? "выбери подходящую звезду из правил мира"
            : profile.StarId;
        string drama = profile.Drama switch
        {
            "soft" => "мягко",
            "mid" => "средне",
            "hot" => "остро",
            "off" => "без хейта",
            _ => profile.Drama,
        };
        string voice = string.IsNullOrWhiteSpace(profile.Voice)
            ? "не задан — следуй тону мира"
            : profile.Voice;
        string text = $$"""
            ПОЛЬЗОВАТЕЛЬСКИЙ СИСТЕМНЫЙ СЛОЙ
            Эти настройки видны игроку и определяют, как AI работает с ним.

            Мир: {{profile.WorldTitle}} ({{profile.WorldId}})
            Описание мира: {{profile.WorldDescription}}
            Градус драмы: {{drama}}
            Голос канала: {{voice}}
            Текущая звезда: {{star}}
            Память звезды об игроке: {{memory}}
            Правила мира:
            {{rules}}
            """;
        return new ChatMessage(ChatRole.System, text);
    }
}
