using Microsoft.Extensions.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI;

public interface IStarAgentInstructions
{
    ChatMessage CreatePlatformPolicy(string turnPolicy, StarAgentProfile profile);

    ChatMessage CreatePlayerOwnedLayer(StarAgentProfile profile);

    string SerializeTexts(IEnumerable<string> texts);

    string SerializeHistory(IReadOnlyList<GamePost> posts, StarAgentProfile profile);

    string SerializeFeedSource(StarFeedSourceContent source);
}
