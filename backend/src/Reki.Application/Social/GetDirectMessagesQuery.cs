using Mediator;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Social;

public sealed record GetDirectMessagesQuery(StarId StarId, WorldId WorldId)
    : IRequest<DirectMessageView>;