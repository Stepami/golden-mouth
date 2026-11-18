using Microsoft.Extensions.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.AI.Comment;

public sealed record StarFeedCommentContext(
    StarFeedContext Feed,
    GamePost SourcePost,
    GameComment SourceComment,
    IReadOnlyList<GameComment> Branch,
    IReadOnlyList<GameComment> ExistingReplies) : IStarFeedContext
{
    public StarAgentProfile Agent => Feed.Agent;

    public IEnumerable<ChatMessage> GetChatMessages(IStarAgentInstructions instructions)
    {
        var messages = Feed.GetChatMessages(instructions);
        var source = SourcePost.ToFeedSourceContent(SourceComment, Branch, ExistingReplies);
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
