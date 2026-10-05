using Mediator;

namespace Reki.Application.Game.Social;

public sealed record GetFriendsQuery(long TelegramUserId) : IRequest<IReadOnlyList<FriendSummary>>;

