using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record GetDirectMessagesQuery(TelegramUserId TelegramUserId, StarId StarId)
    : IRequest<DirectMessageView>;

