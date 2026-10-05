using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record FriendResponse(
    string Id,
    string Name,
    string Avatar,
    string World,
    long Subs,
    string LastPost,
    string Pronoun);

