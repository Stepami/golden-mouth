using Microsoft.Extensions.AI;

namespace Reki.Application.Posts.AI;

public interface IStarFeedContext
{
    StarAgentProfile Agent { get; }

    IEnumerable<ChatMessage> GetChatMessages(IStarAgentInstructions instructions);
}
