using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Serializers;

internal sealed class GamePostEncodeSerializer : IEncodeSerializer<GamePost>
{
    public string Serialize(GamePost item) => item.Text;
}
