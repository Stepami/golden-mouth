using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Application.Social.GetFriends;

public sealed record FriendSummary(
    PlayerId Id,
    string Name,
    string Avatar,
    WorldId World,
    long Subscribers,
    string LastPost,
    string Pronoun);

