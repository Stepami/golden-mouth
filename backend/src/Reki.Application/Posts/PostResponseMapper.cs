using System.Globalization;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.Responses.Comment;
using Reki.Application.Posts.Responses.FeedItem;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Posts;

[Mapper]
public static partial class PostResponseMapper
{
    public static StarFeedSourceComment ToFeedSourceComment(this GameComment comment) =>
        new(comment.Id, comment.ParentCommentId, comment.AuthorId, comment.Text);

    public static StarFeedSourceContent ToFeedSourceContent(
        this GamePost post,
        GameComment? comment,
        IReadOnlyList<GameComment> branch,
        IReadOnlyList<GameComment> existingReplies) =>
        new(
            post.Id,
            post.AuthorId,
            post.Text,
            comment?.ToFeedSourceComment(),
            branch.Select(ToFeedSourceComment).ToArray(),
            existingReplies.Select(ToFeedSourceComment).ToArray());

    public static StarFeedHistoryPost ToFeedHistoryPost(this GamePost post, StarAgentProfile profile)
    {
        var isPlayer = post.AuthorId == profile.World.Player.PlayerId.ToAuthorId();
        var author = profile.World.FindStar(post.AuthorId) ?? WorldStar.DeletedAccount;
        return new StarFeedHistoryPost(
            isPlayer ? null : author.Id,
            isPlayer ? "Игрок" : author.Name,
            post.AuthorId == profile.StarId?.ToAuthorId(),
            post.Text);
    }

    public static FeedItemResponse MapFeedItem(
        this GamePost post,
        World author)
    {
        var resolved = ResolveAuthor(post.AuthorId, author);
        return new(
            FeedItemKind.Post,
            post.Id,
            resolved.Name,
            resolved.Avatar,
            resolved.Role,
            post.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            post.Text,
            post.Likes,
            post.LikedBy,
            post.Views,
            post.Comments.Count,
            post.AuthorId,
            post.RepostId);
    }

    public static PostDetailResponse MapPost(this GamePost post, World world, PlayerId playerId) =>
        new(
            post.Id,
            post.AuthorId,
            post.Text,
            post.Views,
            post.Likes,
            post.LikedBy,
            post.Comments.Select(comment => MapComment(post, comment, world, playerId)).ToArray(),
            post.RepostId);

    private static CommentResponse MapComment(GamePost post, GameComment comment, World world, PlayerId playerId)
    {
        var author = ResolveAuthor(comment.AuthorId, world);
        var reactions = comment.Reactions.Select(reaction => MapReaction(reaction, world, playerId)).ToArray();
        return new CommentResponse(
            comment.Id,
            author.Name,
            author.Avatar,
            author.Handle,
            comment.Text,
            author.Star,
            comment.AuthorId == playerId.ToAuthorId(),
            comment.Hidden,
            comment.ParentCommentId,
            comment.RequestId,
            comment.AuthorId,
            author.Role,
            post.GetBranch(comment.Id).Count - 1,
            reactions);
    }

    private static CommentReactionResponse MapReaction(CommentReaction reaction, World world, PlayerId playerId)
    {
        var owner = ResolveAuthor(reaction.AuthorId, world);
        return new CommentReactionResponse(
            reaction.AuthorId,
            owner.Name,
            owner.Avatar,
            owner.Star,
            reaction.AuthorId == playerId.ToAuthorId(),
            reaction.Emoji);
    }

    private static (string Name, string Avatar, string Role, string Handle, bool Star) ResolveAuthor(AuthorId id, World world)
    {
        if (id == world.Player.PlayerId.ToAuthorId())
        {
            return (world.Player.Name, world.Player.Avatar, world.Player.Role, world.Player.Avatar, false);
        }

        var star = world.FindStar(id);
        if (star is not null)
        {
            return (star.Name, star.AvatarSeed, star.Role, $"@{star.AvatarSeed.TrimStart('@')}", true);
        }

        var deleted = WorldStar.DeletedAccount;
        return (deleted.Name, deleted.AvatarSeed, deleted.Role, string.Empty, false);
    }
}
