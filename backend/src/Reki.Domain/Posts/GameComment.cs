namespace Reki.Domain.Posts;

public sealed record GameComment(
    CommentId Id,
    string Author,
    string Avatar,
    string Handle,
    string Text,
    bool Star,
    bool Me,
    bool Hidden);

