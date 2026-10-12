using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Tests.Common;

namespace Reki.Tests.Posts;

public sealed class AuthorBsonTests
{
    [Test]
    public async Task Bson_PostThread_RoundTripsCanonicalAuthorStringsAsync()
    {
        // Arrange
        var post = CreatePost();

        // Act
        var document = post.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<GamePost>(document);

        // Assert
        await AssertAuthorsAsync(document, restored, post);
    }

    [Test]
    public async Task Bson_PublicationResultPost_RoundTripsCanonicalAuthorStringsAsync()
    {
        // Arrange
        var post = CreatePost();
        var publication = new Publication("publication", PlayerId.From("player_one"), "Post", "hash", true, null, PublicationStatus.Completed, [], new PublishedPost(post, "hook", "name", "✨", []), null, post.WorldId);

        // Act
        var document = publication.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<Publication>(document);

        // Assert
        var result = restored.Result ?? throw new InvalidOperationException("Missing publication result.");
        await AssertAuthorsAsync(document["Result"]["Post"].AsBsonDocument, result.Post, post);
        await Assert.That(restored.Id).IsEqualTo("publication");
    }

    private static GamePost CreatePost()
    {
        var player = PlayerId.From("player_one").ToAuthorId();
        var star = AuthorId.Create(AuthorKind.Star, " star_two ");
        var root = new GameComment(CommentId.From("root"), null, null, star, "Root", false, [new CommentReaction(player, "✨"), new CommentReaction(star, "💡")]);
        var reply = new GameComment(CommentId.From("reply"), root.Id, "request", player, "Reply", false, [new CommentReaction(star, "🌿")]);
        return new GamePost(PostId.From("post"), player, TestGameData.CreateWorld().Id, "Post", 10, [root, reply], TestGameData.PeriodStart, 0, 0.5, 1, false, false, 3);
    }

    private static async Task AssertAuthorsAsync(BsonDocument document, GamePost restored, GamePost expected)
    {
        await Assert.That(document["AuthorId"].BsonType).IsEqualTo(BsonType.String);
        await Assert.That(document["AuthorId"].AsString).IsEqualTo("Player_player_one");
        await Assert.That(restored.AuthorId).IsEqualTo(expected.AuthorId);
        await Assert.That(restored.Version).IsEqualTo(3);
        await Assert.That(restored.Comments.Count).IsEqualTo(2);
        for (var index = 0; index < expected.Comments.Count; index++)
        {
            var storedComment = document["Comments"][index];
            var comment = expected.Comments[index];
            await Assert.That(storedComment["AuthorId"].BsonType).IsEqualTo(BsonType.String);
            await Assert.That(storedComment["AuthorId"].AsString).IsEqualTo(comment.AuthorId.Value);
            await Assert.That(restored.Comments[index].AuthorId).IsEqualTo(comment.AuthorId);
            await Assert.That(restored.Comments[index].ParentCommentId).IsEqualTo(comment.ParentCommentId);
            await Assert.That(restored.Comments[index].RequestId).IsEqualTo(comment.RequestId);
            await Assert.That(restored.Comments[index].Reactions.Count).IsEqualTo(comment.Reactions.Count);
            await AssertReactionsAsync(storedComment, restored.Comments[index], comment);
        }
    }

    private static async Task AssertReactionsAsync(BsonValue storedComment, GameComment restored, GameComment comment)
    {
        for (var reactionIndex = 0; reactionIndex < comment.Reactions.Count; reactionIndex++)
        {
            var storedReaction = storedComment["Reactions"][reactionIndex];
            var reaction = comment.Reactions[reactionIndex];
            await Assert.That(storedReaction["AuthorId"].BsonType).IsEqualTo(BsonType.String);
            await Assert.That(storedReaction["AuthorId"].AsString).IsEqualTo(reaction.AuthorId.Value);
            await Assert.That(restored.Reactions[reactionIndex]).IsEqualTo(reaction);
        }
    }
}
