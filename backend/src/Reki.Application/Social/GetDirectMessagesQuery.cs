using Mediator;

namespace Reki.Application.Social;

public sealed record GetDirectMessagesQuery(long TelegramUserId, string StarId)
    : IRequest<DirectMessageView>;

