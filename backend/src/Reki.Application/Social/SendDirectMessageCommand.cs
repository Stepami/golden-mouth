using Mediator;

namespace Reki.Application.Social;

public sealed record SendDirectMessageCommand(long TelegramUserId, string StarId, string Text)
    : IRequest<SentDirectMessage>;

