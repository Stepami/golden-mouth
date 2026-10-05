using Mediator;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Application.Social;

public sealed record SendDirectMessageCommand(TelegramUserId TelegramUserId, StarId StarId, string Text)
    : IRequest<SentDirectMessage>;

