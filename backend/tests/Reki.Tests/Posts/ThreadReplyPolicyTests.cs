using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class ThreadReplyPolicyTests
{
    private static readonly CommentId Target = CommentId.From("target-comment");
    private static readonly CommentId ReplyComment = CommentId.From("reply-comment");

    [Test]
    public async Task CreateReply_FirstReply_AwardsBonusAndReturnsReactionAsync()
    {
        // Arrange
        GamePost post = CreatePost([]);

        // Act
        ThreadReply reply = ThreadReplyPolicy.CreateReply(post, "key", ReplyComment, Target, 100, 3, 9, 2);

        // Assert
        await Assert.That(reply.BonusAwarded).IsEqualTo(9);
        await Assert.That(reply.AwardedReplies).IsEqualTo(1);
        await Assert.That(reply.SubscriberTotal).IsEqualTo(109);
        await Assert.That(reply.Reaction).IsEqualTo(ThreadReplyPolicy.CommenterReaction);
    }

    [Test]
    public async Task CreateReply_AfterConfiguredLimit_AwardsNothingAndKeepsProgressAsync()
    {
        // Arrange
        GamePost post = CreatePost(
        [
            CreateStoredReply("a", 1),
            CreateStoredReply("b", 2),
            CreateStoredReply("c", 3),
        ]);

        // Act
        ThreadReply reply = ThreadReplyPolicy.CreateReply(post, "d", ReplyComment, Target, 130, 3, 9, 2);

        // Assert
        await Assert.That(reply.BonusAwarded).IsEqualTo(0);
        await Assert.That(reply.AwardedReplies).IsEqualTo(3);
        await Assert.That(reply.SubscriberTotal).IsEqualTo(130);
    }

    [Test]
    public async Task CreateReply_WhenBranchDepthIsOne_ReturnsNoReactionAsync()
    {
        // Arrange
        GamePost post = CreatePost([]);

        // Act
        ThreadReply reply = ThreadReplyPolicy.CreateReply(post, "key", ReplyComment, Target, 100, 3, 9, 1);

        // Assert
        await Assert.That(reply.Reaction).IsNull();
    }

    private static GamePost CreatePost(IReadOnlyList<ThreadReply> replies) =>
        new(
            PostId.From("post-one"),
            PlayerId.From("player-one"),
            "текст поста",
            10,
            [],
            replies,
            TestGameData.PeriodStart,
            0,
            0,
            1,
            false,
            false);

    private static ThreadReply CreateStoredReply(string replyId, int awardedReplies) =>
        new(
            replyId,
            CommentId.From($"comment-{replyId}"),
            Target,
            1,
            9,
            awardedReplies,
            109,
            ThreadReplyPolicy.CommenterReaction);
}
