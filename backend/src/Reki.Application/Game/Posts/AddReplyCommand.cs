using Mediator;

namespace Reki.Application.Game.Posts;

public sealed record AddReplyCommand(long TelegramUserId, string PostId, string Text) : IRequest;

