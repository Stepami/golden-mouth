using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Serializers;

internal sealed class WorldEncodeSerializer : IEncodeSerializer<World>
{
    public string Serialize(World world) =>
        $"{world.Interface.Title}. {world.Interface.Description}. {world.Player.Role}";
}