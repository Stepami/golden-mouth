using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record DirectMessageView(StarConversation Conversation, int RepliesLeft, int Relation, StarPersona Persona);

