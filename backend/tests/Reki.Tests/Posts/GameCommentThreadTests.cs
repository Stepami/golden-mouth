using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class GameCommentThreadTests
{
    private static readonly CommentId RootId = CommentId.From("root");
    private static readonly CommentId ReplyId = CommentId.From("player-reply");
    private static readonly CommentId StarReplyId = CommentId.From("star-reply");

    [Test]
    public async Task GetBranch_ShuffledStorage_ReturnsAncestorsInParentOrderAsync()
    {
        // Arrange
        var root = Comment(RootId, null);
        var reply = Comment(ReplyId, RootId, "request-one", PlayerId.From("player-one").ToAuthorId());
        var followUp = Comment(StarReplyId, ReplyId);
        var post = Post([followUp, root, reply]);

        // Act
        var rootBranch = post.GetBranch(RootId);
        var replyBranch = post.GetBranch(ReplyId);
        var fullBranch = post.GetBranch(StarReplyId);

        // Assert
        await Assert.That(rootBranch.Count).IsEqualTo(1);
        await Assert.That(rootBranch[0].Id).IsEqualTo(RootId);
        await Assert.That(replyBranch.Count).IsEqualTo(2);
        await Assert.That(replyBranch[0].Id).IsEqualTo(RootId);
        await Assert.That(replyBranch[1].Id).IsEqualTo(ReplyId);
        await Assert.That(fullBranch.Count).IsEqualTo(3);
        await Assert.That(fullBranch[0].Id).IsEqualTo(RootId);
        await Assert.That(fullBranch[1].Id).IsEqualTo(ReplyId);
        await Assert.That(fullBranch[2].Id).IsEqualTo(StarReplyId);
    }

    [Test]
    [Arguments("missing")]
    [Arguments("dangling")]
    [Arguments("cycle")]
    public async Task GetBranch_InvalidParentChain_RejectsTreeAsync(string scenario)
    {
        // Arrange
        var comments = scenario switch
        {
            "missing" => Array.Empty<GameComment>(),
            "dangling" => [Comment(RootId, ReplyId)],
            _ => new[] { Comment(RootId, ReplyId), Comment(ReplyId, RootId) },
        };
        var post = Post(comments);

        // Act
        var getBranch = () => post.GetBranch(RootId);

        // Assert
        await Assert.That(getBranch).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AddComments_CompleteTurn_PreservesCanonicalTurnAsync()
    {
        // Arrange
        var playerAuthor = PlayerId.From("player-one").ToAuthorId();
        var starAuthor = TestGameData.StarId.ToAuthorId();
        var reaction = new CommentReaction(starAuthor, "✨");
        var playerReply = Comment(ReplyId, RootId, "request-one", playerAuthor) with { Reactions = [reaction] };
        var starReply = Comment(StarReplyId, ReplyId);
        var additionalRoot = Comment(CommentId.From("other-root"), null);
        var post = Post([Comment(RootId, null)]) with { AuthorId = starAuthor, Version = 4 };

        // Act
        var updated = post.AddComments([playerReply, starReply, additionalRoot]);

        // Assert
        await Assert.That(updated.Version).IsEqualTo(5);
        await Assert.That(updated.WorldId).IsEqualTo(TestGameData.CreateWorld().Id);
        await Assert.That(updated.AuthorId).IsEqualTo(starAuthor);
        await Assert.That(updated.Comments.Count).IsEqualTo(4);
        await Assert.That(updated.FindRequest("request-one")).IsEqualTo(playerReply);
        await Assert.That(updated.FindRequest("REQUEST-ONE")).IsNull();
        await Assert.That(updated.FindRequest("unknown")).IsNull();
        await Assert.That(updated.GetBranch(StarReplyId).Count).IsEqualTo(3);
        await Assert.That(updated.GetBranch(additionalRoot.Id).Count).IsEqualTo(1);
        await Assert.That(playerReply.ParentCommentId).IsEqualTo(RootId);
        await Assert.That(starReply.ParentCommentId).IsEqualTo(ReplyId);
        await Assert.That(starReply.RequestId).IsNull();
        await Assert.That(additionalRoot.RequestId).IsNull();
        await Assert.That(updated.Comments[1].Reactions[0].AuthorId).IsEqualTo(starAuthor);
        await Assert.That(updated.Comments[1].Reactions[0].Emoji).IsEqualTo("✨");
        await Assert.That(updated.Comments[0].Reactions.Count).IsEqualTo(0);
        await Assert.That(updated.Comments[2].Reactions.Count).IsEqualTo(0);
        await Assert.That(post.Comments.Count).IsEqualTo(1);
        await Assert.That(post.Version).IsEqualTo(4);
    }

    [Test]
    [Arguments("existing-id")]
    [Arguments("batch-id")]
    [Arguments("missing-parent")]
    [Arguments("self-parent")]
    [Arguments("existing-request")]
    [Arguments("batch-request")]
    public async Task AddComments_DuplicateIdentityOrInvalidParent_RejectsEntireTurnAsync(string scenario)
    {
        // Arrange
        var post = Post([Comment(RootId, null, "saved-key")]);
        var first = Comment(ReplyId, RootId, "new-key");
        IReadOnlyList<GameComment> additions = scenario switch
        {
            "existing-id" => [Comment(RootId, null)],
            "batch-id" => [first, first],
            "missing-parent" => [Comment(ReplyId, StarReplyId)],
            "self-parent" => [Comment(ReplyId, ReplyId)],
            "existing-request" => [Comment(ReplyId, RootId, "saved-key")],
            _ => [first, Comment(StarReplyId, ReplyId, "new-key")],
        };

        // Act
        var addComments = () => post.AddComments(additions);

        // Assert
        await Assert.That(addComments).Throws<InvalidOperationException>();
        await Assert.That(post.Comments.Count).IsEqualTo(1);
        await Assert.That(post.Version).IsEqualTo(0);
        await Assert.That(post.FindRequest("new-key")).IsNull();
    }

    private static GameComment Comment(CommentId id, CommentId? parent) =>
        Comment(id, parent, null, TestGameData.StarId.ToAuthorId());

    private static GameComment Comment(CommentId id, CommentId? parent, string requestId) =>
        Comment(id, parent, requestId, TestGameData.StarId.ToAuthorId());

    private static GameComment Comment(CommentId id, CommentId? parent, string? requestId, AuthorId author) =>
        new(id, parent, requestId, author, "Comment", false, []);

    private static GamePost Post(IReadOnlyList<GameComment> comments) =>
        new(
            PostId.From("post-one"),
            PlayerId.From("player-one").ToAuthorId(),
            TestGameData.CreateWorld().Id,
            "Post",
            10,
            comments,
            TestGameData.PeriodStart,
            0,
            0.5,
            1,
            false,
            false);
}