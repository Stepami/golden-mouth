using Mediator;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record GetDirectMessagesQuery(StarId StarId)
    : IRequest<DirectMessageView>;

