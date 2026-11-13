using System.Globalization;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.ChannelPost;
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
        World author,
        GamePost? source = null)
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
            CreateReactions(post),
            post.Views,
            post.Comments.Count,
            null,
            MapQuote(post, source, author),
            author.FindStar(post.AuthorId) is not null && post.QuotedPostId is null,
            post.RepostActive,
            post.IsRepost);
    }

    public static ChannelPostResponse MapChannelPost(GamePost post, World world, GamePost? source = null) =>
        new(post.Id, post.Text, post.CreatedAtUtc, post.Views, CreateReactions(post), post.Comments.Count, MapQuote(post, source, world), post.IsRepost);

    public static QuotedPostResponse? MapQuote(GamePost post, GamePost? source, World world)
    {
        if (post.QuotedPostId is not { } id)
        {
            return null;
        }

        var author = source is null ? null : world.FindStar(source.AuthorId);
        return new QuotedPostResponse(id, author?.Name, author?.AvatarSeed, author is null ? null : source?.Text);
    }

    public static IReadOnlyList<ReactionResponse> CreateReactions(this GamePost post) =>
        [new ReactionResponse(post.ReactionEmoji, post.Viral ? 3 : 1)];

    public static PostDetailResponse MapPost(this GamePost post, World world, PlayerId playerId, GamePost? source = null) =>
        new(
            post.Id,
            post.AuthorId,
            post.Text,
            post.Views,
            CreateReactions(post),
            post.Comments.Select(comment => MapComment(post, comment, world, playerId)).ToArray(),
            MapQuote(post, source, world),
            post.IsRepost);

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
