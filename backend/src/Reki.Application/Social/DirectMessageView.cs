using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public sealed record DirectMessageView(StarConversation Conversation, int RepliesLeft, int Relation, WorldStar Star);