using Reki.Domain.Worlds;

namespace Reki.Application.Worlds;

public static class WorldRulesDefaults
{
    public static WorldRules Create(string playerId, string worldId)
    {
        string title = worldId switch
        {
            "stage" => "Сцена",
            "academy" => "Академия",
            "it" => "Айти-тусовка",
            "stream" => "Стрим",
            _ => "Свой мир",
        };
        string description = worldId switch
        {
            "stage" => "Айдол-агентство: трейни, дебют, камбэки и фансайты.",
            "academy" => "Школьная академия: клубы, соперники и фестиваль.",
            "it" => "Блогеры, разоблачения, коалиции и стартапы.",
            "stream" => "Стримеры, донаты, рейды и турниры.",
            _ => "Мир, который создаёт игрок.",
        };

        return new WorldRules(
            playerId,
            worldId,
            title,
            worldId == "own" ? ["🏫 моя школа", "💻 IT-драма", "🏰 фэнтези-академия"] : [],
            description,
            [
                new WorldRule("Контент подходит для 14+", true),
                new WorldRule("Звёзды реагируют только на пост и его тему", true),
                new WorldRule("Пиши в голосе своего канала", false),
            ]);
    }
}