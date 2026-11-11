using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Serializers;

internal sealed class WorldEncodeSerializer : IEncodeSerializer<World>
{
    public string Serialize(World item) =>
        $"{item.Interface.Title}. {item.Interface.Description}. {item.Player.Role}";
}