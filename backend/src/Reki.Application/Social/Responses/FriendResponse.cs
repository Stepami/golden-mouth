namespace Reki.Application.Social.Responses;

public sealed record FriendResponse(
    string Id,
    string Name,
    string Avatar,
    string World,
    long Subs,
    string LastPost,
    string Pronoun);

