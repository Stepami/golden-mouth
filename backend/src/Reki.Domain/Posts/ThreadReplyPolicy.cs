namespace Reki.Domain.Posts;

/// <summary>
/// Pure rule for one player reply in a post thread: how many replies already earned the bonus,
/// whether this one earns it, and whether the commenter reaction completes the branch depth.
/// </summary>
public static class ThreadReplyPolicy
{
    /// <summary>Deterministic safe commenter reaction; the model is never called for replies.</summary>
    public const string CommenterReaction = "🔥";

    public static ThreadReply CreateReply(
        GamePost post,
        string replyId,
        CommentId commentId,
        CommentId targetCommentId,
        long subscriberTotalBefore,
        int bonusReplies,
        long subscriberBonus,
        int branchDepth)
    {
        IReadOnlyList<ThreadReply> replies = post.GetThreadReplies();
        int awardedSoFar = replies.Count == 0
            ? 0
            : replies.Max(item => item.AwardedReplies);
        bool awards = awardedSoFar < bonusReplies;
        long bonus = awards ? subscriberBonus : 0;
        int awardedReplies = awards ? awardedSoFar + 1 : awardedSoFar;
        int depth = 1;
        string? reaction = depth + 1 == branchDepth ? CommenterReaction : null;
        return new ThreadReply(
            replyId,
            commentId,
            targetCommentId,
            depth,
            bonus,
            awardedReplies,
            subscriberTotalBefore + bonus,
            reaction);
    }
}
