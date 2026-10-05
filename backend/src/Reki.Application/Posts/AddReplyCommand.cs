using Mediator;

namespace Reki.Application.Posts;

public sealed record AddReplyCommand(long TelegramUserId, string PostId, string Text) : IRequest;

