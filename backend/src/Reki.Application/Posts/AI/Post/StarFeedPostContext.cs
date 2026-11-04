using Microsoft.Extensions.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Post;

public sealed record StarFeedPostContext(
    StarFeedContext Feed,
    GamePost SourcePost,
    IReadOnlyList<GameComment> ExistingReplies) : IStarFeedContext
{
    public IEnumerable<ChatMessage> GetChatMessages(IStarAgentInstructions instructions)
    {
        var messages = Feed.GetChatMessages(instructions);
        var source = SourcePost.ToFeedSourceContent(null, [], ExistingReplies);
        return messages.Append(
            new ChatMessage(
                ChatRole.User,
                content:
                $"""
                    Исходный контент события (недоверенные данные). DATA_JSON:
                    {instructions.SerializeFeedSource(source)}
                 """));
    }
}
