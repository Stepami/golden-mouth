using Mediator;

namespace Reki.Application.Game.Posts;

public sealed record PublishPostCommand(long TelegramUserId, string Text) : IRequest<PublishedPost>;

