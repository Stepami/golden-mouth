using Mediator;

namespace Reki.Application.Game.Social;

public sealed record GetDirectMessagesQuery(long TelegramUserId, string StarId)
    : IRequest<DirectMessageView>;

