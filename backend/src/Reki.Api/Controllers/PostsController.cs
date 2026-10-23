using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Requests.Posts;
using Reki.Application.Posts;
using Reki.Application.Posts.GetPostDetail;
using Reki.Application.Posts.GetPublicationStatus;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Application.Posts.Responses.ChannelPost;
using Reki.Application.Posts.Responses.FeedItem;
using Reki.Application.Posts.Responses.LiveEvent;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.Posts.Responses.PublicationStatus;
using Reki.Application.Posts.Responses.PublicationStatusLiveEvent;
using Reki.Application.Posts.Responses.ReplyLiveEvent;
using Reki.Application.RekiContext;
using Reki.Application.Responses;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PostsController(ISender sender) : ControllerBase
{
    [HttpPost("posts/publications", Name = "GeneratePublication")]
    [ActionName("GeneratePublication")]
    public ServerSentEventsResult<PublicationStatusLiveEvent> GeneratePublication(
        StartPublicationRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var result = sender.CreateStream(
            request.ToGeneratePublication(worldId),
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
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PublishPost(publicationId, worldId),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPost("posts/{postId}/replies", Name = "ReplyToPost")]
    [ActionName("ReplyToPost")]
    public ServerSentEventsResult<ReplyLiveEvent> ReplyToPost(
        string postId,
        ReplyRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var result = sender.CreateStream(
            request.ToAddReply(postId, worldId),
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
            request.ToQuery(feed: true),
            cancellationToken);
        var worlds = await rekiContext.Worlds;
        IReadOnlyList<FeedItemResponse> response =
            [.. result.Items.Select(post => PostResponseMapper.MapFeedItem(post, worlds[post.WorldId]))];
        return TypedResults.Ok(new PageResponse<FeedItemResponse>(response, result.NextCursor?.ToString()));
    }

    [HttpGet("posts/mine", Name = "GetChannelPosts")]
    [ActionName("GetChannelPosts")]
    public async Task<Ok<PageResponse<ChannelPostResponse>>> GetChannelPostsAsync(
        [FromQuery] WorldPostsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            request.ToQuery(),
            cancellationToken);
        IReadOnlyList<ChannelPostResponse> response = [.. result.Items.Select(PostResponseMapper.MapChannelPost)];
        return TypedResults.Ok(new PageResponse<ChannelPostResponse>(response, result.NextCursor?.ToString()));
    }

    [HttpGet("posts/{postId}", Name = "GetPost")]
    [ActionName("GetPost")]
    public async Task<Ok<PostDetailResponse>> GetPostAsync(
        string postId,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PostDetailQuery(PostId.From(postId), worldId),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}
