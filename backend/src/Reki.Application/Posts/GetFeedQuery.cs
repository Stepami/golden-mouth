using Mediator;
using Reki.Contracts.Posts;
using Reki.Domain.Player;

namespace Reki.Application.Posts;

public sealed record GetFeedQuery(TelegramUserId TelegramUserId) : IRequest<IReadOnlyList<FeedItemResponse>>;

