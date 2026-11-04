using Microsoft.Extensions.AI;

namespace Reki.Application.Posts.AI;

public interface IStarFeedContext
{
    IEnumerable<ChatMessage> GetChatMessages(IStarAgentInstructions instructions);
}
