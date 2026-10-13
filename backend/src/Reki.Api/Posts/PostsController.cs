using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Common;
using Reki.Application.Posts;
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
            new GeneratePublicationCommand(
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
            new GetPublicationStatusQuery(publicationId), cancellationToken);
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
            new PublishPostCommand(publicationId, Domain.Worlds.WorldId.From(worldId)),
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
            new AddReplyCommand(
                PostId.From(postId),
                CommentId.From(request.CommentId),
                request.RequestId,
                request.Text,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.ServerSentEvents(result);
    }

    [HttpGet("feed", Name = "GetFeed")]
    [ActionName("GetFeed")]
    public async Task<Ok<IReadOnlyList<FeedItemResponse>>> GetFeedAsync(
        [FromQuery] string worldId,
        [FromServices] IRekiContext rekiContext,
        CancellationToken cancellationToken)
    {
        const int feedSize = 20;
        var result = await sender.Send(
            new GetChannelPostsQuery(Domain.Worlds.WorldId.From(worldId), feedSize, Feed: true),
            cancellationToken);
        var worlds = await rekiContext.Worlds;
        IReadOnlyList<FeedItemResponse> response =
            [.. result.Select(post => PostContractMapper.MapFeedItem(post, worlds[post.WorldId], true))];
        return TypedResults.Ok(response);
    }

    [HttpGet("posts/mine", Name = "GetChannelPosts")]
    [ActionName("GetChannelPosts")]
    public async Task<Ok<IReadOnlyList<ChannelPostResponse>>> GetChannelPostsAsync(
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetChannelPostsQuery(Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        IReadOnlyList<ChannelPostResponse> response = result.Select(PostContractMapper.MapChannelPost).ToArray();
        return TypedResults.Ok(response);
    }

    [HttpGet("posts/{postId}", Name = "GetPost")]
    [ActionName("GetPost")]
    public async Task<Ok<PostDetailResponse>> GetPostAsync(
        string postId,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPostDetailQuery(PostId.From(postId), Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}