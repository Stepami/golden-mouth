using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;

namespace Reki.Api.Posts;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PostsController(
    ISender sender) : ControllerBase
{
    [HttpPost("posts/publications", Name = "GeneratePublication")]
    [ActionName("GeneratePublication")]
    public ServerSentEventsResult<PublicationStatusLiveEvent> GeneratePublication(
        StartPublicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = sender.CreateStream(
            new GeneratePublicationCommand(
                request.PublicationId,
                request.Text,
                request.OwnWords,
                request.WantId),
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
            new GetPublicationQuery(publicationId),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPost("posts/publications/{publicationId}/moderation", Name = "ModeratePublication")]
    [ActionName("ModeratePublication")]
    public ServerSentEventsResult<LiveEvent> ModeratePublication(
        string publicationId,
        CancellationToken cancellationToken)
    {
        var result = sender.CreateStream(
            new PublishPostCommand(publicationId),
            cancellationToken);
        return TypedResults.ServerSentEvents(result);
    }

    [HttpPost("posts/{postId}/replies", Name = "ReplyToPost")]
    [ActionName("ReplyToPost")]
    public async Task<Ok<ReplyResponse>> ReplyToPostAsync(
        string postId,
        ReplyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddReplyCommand(
                PostId.From(postId),
                CommentId.From(request.CommentId),
                request.ReplyId,
                request.Text),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpGet("feed", Name = "GetFeed")]
    [ActionName("GetFeed")]
    public async Task<Ok<IReadOnlyList<FeedItemResponse>>> GetFeedAsync(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetFeedQuery(),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpGet("posts/mine", Name = "GetChannelPosts")]
    [ActionName("GetChannelPosts")]
    public async Task<Ok<IReadOnlyList<ChannelPostResponse>>> GetChannelPostsAsync(
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetChannelPostsQuery(),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpGet("posts/{postId}", Name = "GetPost")]
    [ActionName("GetPost")]
    public async Task<Ok<PostDetailResponse>> GetPostAsync(
        string postId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetPostQuery(PostId.From(postId)),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}
