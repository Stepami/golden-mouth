namespace Reki.Application.Posts.Responses;

/// <summary>Реакция на комментарий с готовыми данными автора.</summary>
public sealed record CommentReactionResponse(
    string AuthorId,
    string Author,
    string Avatar,
    bool Star,
    bool Me,
    string Emoji);