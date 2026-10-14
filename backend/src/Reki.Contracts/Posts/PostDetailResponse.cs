namespace Reki.Contracts.Posts;

public sealed record PostDetailResponse(
    string PostId,
    string AuthorId,
    string Text,
    long Views,
    IReadOnlyList<ReactionResponse> Reactions,
    IReadOnlyList<CommentResponse> Comments);
