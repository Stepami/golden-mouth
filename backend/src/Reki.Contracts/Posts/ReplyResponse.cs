namespace Reki.Contracts.Posts;

/// <summary>Persisted outcome of one thread reply; a retry with the same replyId returns it unchanged.</summary>
public sealed record ReplyResponse(
    string ReplyId,
    string CommentId,
    long SubscriberBonus,
    int AwardedReplies,
    int ReplyLimit,
    long SubscriberTotal,
    string? Reaction);
