using Mediator;

namespace Reki.Application.Social.GetFriends;

public sealed record FriendsQuery : IQuery<IReadOnlyList<FriendSummary>>;

