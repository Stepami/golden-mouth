namespace Reki.Domain.Posts;

/// <summary>
/// Saved outcome of one player reply in a post thread. Persisted together with the reply comment
/// so that a retry with the same <see cref="ReplyId"/> returns exactly the same result.
/// </summary>
public sealed record ThreadReply(
    string ReplyId,
    CommentId CommentId,
    CommentId TargetCommentId,
    int Depth,
    long BonusAwarded,
    int AwardedReplies,
    long SubscriberTotal,
    string? Reaction);
