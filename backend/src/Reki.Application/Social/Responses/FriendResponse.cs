using Reki.Domain.Player;

namespace Reki.Application.Social.Responses;

public sealed record FriendResponse(
    PlayerId Id,
    string Name,
    string Avatar,
    string World,
    long Subs,
    string LastPost,
    string Pronoun);

