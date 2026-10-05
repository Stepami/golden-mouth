namespace Reki.Domain.Game.Posts;

public sealed record GameComment(
    string Id,
    string Author,
    string Avatar,
    string Handle,
    string Text,
    bool Star,
    bool Me,
    bool Hidden);

