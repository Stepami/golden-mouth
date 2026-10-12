using System.Globalization;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Social;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts;

public static class PostContractMapper
{
    public static PublicationStatusResponse MapPublication(Publication publication, World world) =>
        new(
            publication.Id,
            publication.Status,
            publication.Result is null ? null : MapResult(publication.Result, world));

    public static FeedItemResponse MapFeedItem(
        GamePost post,
        Reki.Domain.Worlds.World author,
        bool canReply) =>
        new(
            FeedItemKind.Post,
            post.Id.Value,
            ResolveAuthor(post.AuthorId, author).Name,
            ResolveAuthor(post.AuthorId, author).Avatar,
            ResolveAuthor(post.AuthorId, author).Role,
            post.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            post.Text,
            CreateReactions(post),
            post.Views,
            post.Comments.Count,
            null,
            canReply);

    public static ChannelPostResponse MapChannelPost(GamePost post) =>
        new(
            post.Id.Value,
            post.Text,
            post.CreatedAtUtc,
            post.Views,
            CreateReactions(post),
            post.Comments.Count);

    public static IReadOnlyList<ReactionResponse> CreateReactions(GamePost post) =>
        [new ReactionResponse("🔥", post.Viral ? 3 : 1)];

    public static IReadOnlyList<LiveEvent> MapLiveEvents(PublishedPost published, World world, PlayerId playerId)
    {
        var comment = published.Post.Comments[0];
        return
        [
            new ViewsLiveEvent(Math.Min(48, published.Post.Views)),
            new ReactionLiveEvent(published.Reaction),
            new CommentLiveEvent(
                MapComment(published.Post, comment, world, playerId)),
            new TypingLiveEvent(published.StarName),
            new ResultLiveEvent(MapResult(published, world)),
        ];
    }

    public static PostDetailResponse MapPost(GamePost post, World world, PlayerId playerId) =>
        new(
            post.Id.Value,
            post.Text,
            post.Views,
            CreateReactions(post),
            post.Comments.Select(comment => MapComment(post, comment, world, playerId)).ToArray(),
            world.Player.PlayerId == playerId);

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
            comment.AuthorId.Value.Item1 == AuthorKind.Star,
            comment.AuthorId == playerId.ToAuthor(),
            comment.Hidden,
            comment.ParentCommentId?.Value,
            comment.RequestId,
            comment.AuthorId.Value.Item2,
            author.Role,
            post.GetBranch(comment.Id).Count - 1,
            reactions);
    }

    private static CommentReactionResponse MapReaction(CommentReaction reaction, World world, PlayerId playerId)
    {
        var owner = ResolveAuthor(reaction.AuthorId, world);
        return new CommentReactionResponse(
            reaction.AuthorId.Value.Item2,
            owner.Name,
            owner.Avatar,
            reaction.AuthorId.Value.Item1 == AuthorKind.Star,
            reaction.AuthorId == playerId.ToAuthor(),
            reaction.Emoji);
    }

    private static (string Name, string Avatar, string Role, string Handle) ResolveAuthor(AuthorId id, World world)
    {
        if (id == world.Player.PlayerId.ToAuthor())
        {
            return (world.Player.Name, world.Player.Avatar, world.Player.Role, world.Player.Avatar);
        }

        var star = id.Value.Item1 == AuthorKind.Star ? world.FindStar(StarId.From(id.Value.Item2)) : null;
        return star is null
            ? throw new InvalidOperationException("The author does not belong to the world.")
            : (star.Name, star.AvatarSeed, star.Role, $"@{star.Id.Value}");
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