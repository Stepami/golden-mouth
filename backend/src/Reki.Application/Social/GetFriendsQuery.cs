using Mediator;
using Reki.Domain.Player;

namespace Reki.Application.Social;

public sealed record GetFriendsQuery(TelegramUserId TelegramUserId) : IRequest<IReadOnlyList<FriendSummary>>;

