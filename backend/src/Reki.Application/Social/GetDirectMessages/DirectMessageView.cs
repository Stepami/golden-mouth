using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social.GetDirectMessages;

public sealed record DirectMessageView(StarConversation Conversation, int RepliesLeft, int Relation, WorldStar Star);