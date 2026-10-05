namespace Reki.Application.Social;

public sealed record FriendSummary(
    string Id,
    string Name,
    string Avatar,
    string World,
    long Subscribers,
    string LastPost,
    string Pronoun);

