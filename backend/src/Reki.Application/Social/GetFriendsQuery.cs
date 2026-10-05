using Mediator;

namespace Reki.Application.Social;

public sealed record GetFriendsQuery(long TelegramUserId) : IRequest<IReadOnlyList<FriendSummary>>;

