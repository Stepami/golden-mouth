using Mediator;

namespace Reki.Application.Social;

public sealed record GetFriendsQuery : IRequest<IReadOnlyList<FriendSummary>>;

