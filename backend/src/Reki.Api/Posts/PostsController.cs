using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Common.Exceptions;
using Reki.Application.Player;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Posts;

namespace Reki.Api.Posts;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PostsController(
    ISender sender,
    ApiMapper mapper) : ControllerBase
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
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var result = await sender.Send(
            new GetPublicationQuery(publicationId, player.Id),
            cancellationToken) ?? throw new ResourceNotFoundException("Publication not found.");
        return TypedResults.Ok(PostContractMapper.MapPublication(result));
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
    public async Task<Ok<ReplyResponse>> ReplyToPostAsync(
        string postId,
        ReplyRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddReplyCommand(
                PostId.From(postId),
                CommentId.From(request.CommentId),
                request.ReplyId,
                request.Text,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpGet("feed", Name = "GetFeed")]
    [ActionName("GetFeed")]
    public async Task<Ok<IReadOnlyList<FeedItemResponse>>> GetFeedAsync(
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetFeedQuery(Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(result);
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
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var result = await sender.Send(
            new GetPostQuery(PostId.From(postId), player.Id, Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapPostDetail(result));
    }
}