using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record GetFeedQuery(TelegramUserId TelegramUserId) : IRequest<FeedView>;

