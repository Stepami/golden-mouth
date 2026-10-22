using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.GeneratePublicationCommand;
using Reki.Application.Posts.GetPostDetail;
using Reki.Application.Posts.GetPublicationStatus;
using Reki.Application.Posts.GetWorldPosts;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Contracts.Common;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;

namespace Reki.Api.Posts;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PostsController(ISender sender) : ControllerBase
{
    [HttpPost("posts/publications", Name = "GeneratePublication")]
    [ActionName("GeneratePublication")]
    public ServerSentEventsResult<PublicationStatusLiveEvent> GeneratePublication(
        StartPublicationRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = sender.CreateStream(
            new GeneratePublication(
                request.PublicationId,
                request.Text,
                request.OwnWords,
                request.WantId,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.ServerSentEvents(result);
    }

    [HttpGet("posts/publications/{publicationId}", Name = "GetPublicationStatus")]
    [ActionName("GetPublicationStatus")]
    public async Task<Ok<PublicationStatusResponse>> GetPublicationStatusAsync(
        string publicationId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PublicationStatusQuery(publicationId), cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPost("posts/publications/{publicationId}/moderation", Name = "ModeratePublication")]
    [ActionName("ModeratePublication")]
    public async Task<Ok<IReadOnlyList<LiveEvent>>> ModeratePublicationAsync(
        string publicationId,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PublishPost(publicationId, Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPost("posts/{postId}/replies", Name = "ReplyToPost")]
    [ActionName("ReplyToPost")]
    public ServerSentEventsResult<ReplyLiveEvent> ReplyToPost(
        string postId,
        ReplyRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = sender.CreateStream(
            new AddReply(
                PostId.From(postId),
                request.CommentId is { } commentId ? CommentId.From(commentId) : null,
                request.RequestId,
                request.Text,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.ServerSentEvents(result);
    }

    [HttpGet("feed", Name = "GetFeed")]
    [ActionName("GetFeed")]
    public async Task<Ok<PageResponse<FeedItemResponse>>> GetFeedAsync(
        [FromQuery] WorldPostsRequest request,
        [FromServices] IRekiContext rekiContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new WorldPostsQuery(Domain.Worlds.WorldId.From(request.WorldId), request.PageNumber, request.PageSize, Feed: true),
            cancellationToken);
        var worlds = await rekiContext.Worlds;
        IReadOnlyList<FeedItemResponse> response =
            [.. result.Items.Select(post => PostContractMapper.MapFeedItem(post, worlds[post.WorldId]))];
        return TypedResults.Ok(new PageResponse<FeedItemResponse>(response, result.PageNumber, result.HasNext));
    }

    [HttpGet("posts/mine", Name = "GetChannelPosts")]
    [ActionName("GetChannelPosts")]
    public async Task<Ok<PageResponse<ChannelPostResponse>>> GetChannelPostsAsync(
        [FromQuery] WorldPostsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new WorldPostsQuery(Domain.Worlds.WorldId.From(request.WorldId), request.PageNumber, request.PageSize),
            cancellationToken);
        IReadOnlyList<ChannelPostResponse> response = [.. result.Items.Select(PostContractMapper.MapChannelPost)];
        return TypedResults.Ok(new PageResponse<ChannelPostResponse>(response, result.PageNumber, result.HasNext));
    }

    [HttpGet("posts/{postId}", Name = "GetPost")]
    [ActionName("GetPost")]
    public async Task<Ok<PostDetailResponse>> GetPostAsync(
        string postId,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PostDetailQuery(PostId.From(postId), Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}
