using System.Globalization;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

internal static class PostContractMapper
{
    public static PublicationStatusResponse MapPublication(Publication publication) =>
        new(
            publication.Id,
            publication.Status,
            publication.Result is null ? null : MapResult(publication.Result));

    public static FeedItemResponse MapFeedItem(
        GamePost post,
        PlayerProfile author,
        bool canReply) =>
        new(
            FeedItemKind.Post,
            post.Id.Value,
            author.Name,
            author.Avatar,
            author.Role,
            post.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            post.Text,
            CreateReactions(post),
            post.Views,
            post.GetComments().Count,
            null,
            canReply);

    public static ChannelPostResponse MapChannelPost(GamePost post) =>
        new(
            post.Id.Value,
            post.Text,
            post.CreatedAtUtc,
            post.Views,
            CreateReactions(post),
            post.GetComments().Count);

    public static PostDetailResponse MapPostDetail(GamePost post, bool canReply) =>
        new(
            post.Id.Value,
            post.Text,
            post.Views,
            CreateReactions(post),
            post.GetComments()
                .Select(comment => new CommentResponse(
                    comment.Id.Value,
                    comment.Author,
                    comment.Avatar,
                    comment.Handle,
                    comment.Text,
                    comment.Star,
                    canReply && comment.Me,
                    comment.Hidden))
                .ToArray(),
            canReply);

    public static IReadOnlyList<LiveEvent> MapLiveEvents(PublishedPost published)
    {
        var comment = published.Post.Comments[0];
        return
        [
            new ViewsLiveEvent(Math.Min(48, published.Post.Views)),
            new ReactionLiveEvent(published.Reaction),
            new CommentLiveEvent(
                new CommentResponse(
                    comment.Id.Value,
                    comment.Author,
                    comment.Avatar,
                    comment.Handle,
                    comment.Text,
                    comment.Star,
                    comment.Me,
                    comment.Hidden)),
            new TypingLiveEvent(published.StarName),
            new ResultLiveEvent(MapResult(published)),
        ];
    }

    private static PostResultResponse MapResult(PublishedPost source)
    {
        var post = source.Post;
        var bestComment = post.Comments.FirstOrDefault();
        return new PostResultResponse(
            post.Id.Value,
            post.SubscriberDelta,
            post.Viral ? "Пост залетел" : "Канал растёт",
            source.Reasons
                .Select(reason => new WhyResponse(reason.Icon, reason.Label, $"+{post.SubscriberDelta}"))
                .ToArray(),
            post.Viral ? [new RibbonResponse("🚀", "залёт", null)] : [],
            bestComment is null ? null : new BestCommentResponse(bestComment.Avatar, bestComment.Text),
            post.Viral,
            post.Flop,
            post.Views,
            source.Hook);
    }

    private static ReactionResponse[] CreateReactions(GamePost post) =>
        [new ReactionResponse("🔥", post.Viral ? 3 : 1)];
}
