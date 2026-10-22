using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social.GetDirectMessages;

public sealed record DirectMessagesQuery(StarId StarId, WorldId WorldId)
    : IQuery<DirectMessageView>;