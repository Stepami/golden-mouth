using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record DirectMessageView(StarConversation Conversation, int RepliesLeft);

