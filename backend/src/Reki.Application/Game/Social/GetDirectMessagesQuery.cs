using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record GetDirectMessagesQuery(long TelegramUserId, string StarId)
    : IRequest<DirectMessageView>;

