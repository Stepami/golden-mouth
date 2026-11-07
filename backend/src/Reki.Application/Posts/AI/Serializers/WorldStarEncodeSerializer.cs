using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Serializers;

internal sealed class WorldStarEncodeSerializer : IEncodeSerializer<WorldStar>
{
    public string Serialize(WorldStar item) =>
        $"Имя: {item.Name}\nРоль: {item.Role}\nОписание: {item.About}";
}
