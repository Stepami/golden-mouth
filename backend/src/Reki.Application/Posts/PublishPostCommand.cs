using Mediator;

namespace Reki.Application.Posts;

public sealed record PublishPostCommand(long TelegramUserId, string Text) : IRequest<PublishedPost>;

