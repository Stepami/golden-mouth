using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Serializers;

internal sealed class GameCommentEncodeSerializer : IEncodeSerializer<GameComment>
{
    public string Serialize(GameComment item) => item.Text;
}
