using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record CommentResponse(
    string Id,
    string Author,
    string Avatar,
    string Handle,
    string Text,
    bool Star = false,
    bool Me = false,
    bool Hidden = false);

