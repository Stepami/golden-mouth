namespace Reki.Contracts.Posts;

public sealed record CommentResponse(
    string Id,
    string Author,
    string Avatar,
    string Handle,
    string Text,
    bool Star,
    bool Me,
    bool Hidden,
    string? ParentCommentId,
    string? RequestId,
    string AuthorId,
    string Role,
    int Depth,
    IReadOnlyList<CommentReactionResponse> Reactions)
{
    public string AppearanceProfile { get; init; } = "neutral";

    public string AgeBand { get; init; } = string.Empty;
}