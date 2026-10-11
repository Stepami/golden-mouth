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
        var replies = post.GetThreadReplies();
        var awardedSoFar = replies.Count == 0
            ? 0
            : replies.Max(item => item.AwardedReplies);
        var awards = awardedSoFar < bonusReplies;
        var bonus = awards ? subscriberBonus : 0;
        var awardedReplies = awards ? awardedSoFar + 1 : awardedSoFar;
        var depth = 1;
        var reaction = depth + 1 == branchDepth ? CommenterReaction : null;
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
