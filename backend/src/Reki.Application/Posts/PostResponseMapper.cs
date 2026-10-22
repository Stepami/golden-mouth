using System.Globalization;
using Reki.Application.Posts.Responses;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Riok.Mapperly.Abstractions;

namespace Reki.Application.Posts;

[Mapper]
public static partial class PostResponseMapper
{
    public static PublicationStatusResponse MapPublication(this Publication publication, World world) =>
        new(
            publication.Id,
            publication.Status,
            publication.Result is null ? null : MapResult(publication.Result, world),
            publication.Failure);

    public static FeedItemResponse MapFeedItem(
        this GamePost post,
        World author)
    {
        var resolved = ResolveAuthor(post.AuthorId, author);
        return new(
            FeedItemKind.Post,
            post.Id.Value,
            resolved.Name,
            resolved.Avatar,
            resolved.Role,
            post.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            post.Text,
            CreateReactions(post),
            post.Views,
            post.Comments.Count,
            null);
    }

    [MapProperty("Id.Value", nameof(ChannelPostResponse.PostId))]
    [MapProperty("Comments.Count", nameof(ChannelPostResponse.CommentCount))]
    [MapPropertyFromSource(nameof(ChannelPostResponse.Reactions), Use = nameof(CreateReactions))]
    [MapperIgnoreSource(nameof(GamePost.AuthorId))]
    [MapperIgnoreSource(nameof(GamePost.WorldId))]
    [MapperIgnoreSource(nameof(GamePost.SubscriberDelta))]
    [MapperIgnoreSource(nameof(GamePost.Quality))]
    [MapperIgnoreSource(nameof(GamePost.ReachRatio))]
    [MapperIgnoreSource(nameof(GamePost.Viral))]
    [MapperIgnoreSource(nameof(GamePost.Flop))]
    [MapperIgnoreSource(nameof(GamePost.Version))]
    public static partial ChannelPostResponse MapChannelPost(this GamePost post);

    public static IReadOnlyList<ReactionResponse> CreateReactions(this GamePost post) =>
        [new ReactionResponse("🔥", post.Viral ? 3 : 1)];

    public static IReadOnlyList<LiveEvent> MapLiveEvents(this PublishedPost published, World world, PlayerId playerId)
    {
        var comment = published.Post.Comments[0];
        return
        [
            new ViewsLiveEvent(Math.Min(48, published.Post.Views)),
            new ReactionLiveEvent(published.Reaction),
            new CommentLiveEvent(
                MapComment(published.Post, comment, world, playerId)),
            new TypingLiveEvent(ResolveAuthor(comment.AuthorId, world).Name),
            new ResultLiveEvent(MapResult(published, world)),
        ];
    }

    public static PostDetailResponse MapPost(this GamePost post, World world, PlayerId playerId) =>
        new(
            post.Id.Value,
            ResolveAuthor(post.AuthorId, world).Id,
            post.Text,
            post.Views,
            CreateReactions(post),
            post.Comments.Select(comment => MapComment(post, comment, world, playerId)).ToArray());

    private static CommentResponse MapComment(GamePost post, GameComment comment, World world, PlayerId playerId)
    {
        var author = ResolveAuthor(comment.AuthorId, world);
        var reactions = comment.Reactions.Select(reaction => MapReaction(reaction, world, playerId)).ToArray();
        return new CommentResponse(
            comment.Id.Value,
            author.Name,
            author.Avatar,
            author.Handle,
            comment.Text,
            author.Star,
            comment.AuthorId == playerId.ToAuthorId(),
            comment.Hidden,
            comment.ParentCommentId?.Value,
            comment.RequestId,
            author.Id,
            author.Role,
            post.GetBranch(comment.Id).Count - 1,
            reactions);
    }

    private static CommentReactionResponse MapReaction(CommentReaction reaction, World world, PlayerId playerId)
    {
        var owner = ResolveAuthor(reaction.AuthorId, world);
        return new CommentReactionResponse(
            owner.Id,
            owner.Name,
            owner.Avatar,
            owner.Star,
            reaction.AuthorId == playerId.ToAuthorId(),
            reaction.Emoji);
    }

    private static (string Id, string Name, string Avatar, string Role, string Handle, bool Star) ResolveAuthor(AuthorId id, World world)
    {
        if (id == world.Player.PlayerId.ToAuthorId())
        {
            return (world.Player.PlayerId.Value, world.Player.Name, world.Player.Avatar, world.Player.Role, world.Player.Avatar, false);
        }

        var star = world.FindStar(id);
        if (star is not null)
        {
            return (star.Id.Value, star.Name, star.AvatarSeed, star.Role, $"@{star.AvatarSeed.TrimStart('@')}", true);
        }

        var deleted = WorldStar.DeletedAccount;
        return (id.Value, deleted.Name, deleted.AvatarSeed, deleted.Role, string.Empty, false);
    }

    private static PostResultResponse MapResult(PublishedPost source, World world)
    {
        var post = source.Post;
        var bestComment = post.Comments.FirstOrDefault();
        var whys = source.Reasons.Select(reason => new WhyResponse(reason.Icon, reason.Label, $"+{post.SubscriberDelta}")).ToArray();
        var best = bestComment is null ? null : new BestCommentResponse(ResolveAuthor(bestComment.AuthorId, world).Avatar, bestComment.Text);
        return new PostResultResponse(
            post.Id.Value,
            post.SubscriberDelta,
            post.Viral ? "Пост залетел" : "Канал растёт",
            whys,
            post.Viral ? [new RibbonResponse("🚀", "залёт", null)] : [],
            best,
            post.Viral,
            post.Flop,
            post.Views,
            source.Hook);
    }
}
