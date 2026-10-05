using Mediator;
using Reki.Domain.Game;

namespace Reki.Application.Game;

public sealed record FriendSummary(
    string Id,
    string Name,
    string Avatar,
    string World,
    long Subscribers,
    string LastPost,
    string Pronoun);

