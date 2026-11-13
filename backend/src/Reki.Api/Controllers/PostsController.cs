using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Requests.Posts;
using Reki.Application.Posts;
using Reki.Application.Posts.CreatePostCommand;
using Reki.Application.Posts.GetPostDetail;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.ChannelPost;
using Reki.Application.Posts.Responses.FeedItem;
using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.Posts.Responses.Reply.Generation;
using Reki.Application.RekiContext;
using Reki.Application.Responses;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("posts")]
public sealed class PostsController(ISender sender) : ControllerBase
{
    [HttpPost(Name = "CreatePost")]
    [ActionName("CreatePost")]
    public async Task<Ok<CreatePostResponse>> CreatePostAsync(
        CreatePostRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(new CreatePost(request.Text, request.OwnWords, request.WantId, worldId, request.RepostId), cancellationToken));

    [HttpPost("{postId}/replies", Name = "ReplyToPost")]
    [ActionName("ReplyToPost")]
    public async Task<Ok<ReplyResponse>> ReplyToPostAsync(
        PostId postId,
        ReplyRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            request.ToAddReply(postId, worldId),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPost("{postId}/replies/generation", Name = "GenerateReply")]
    [ActionName("GenerateReply")]
    public ServerSentEventsResult<ReplyGenerationLiveEvent> GenerateReply(
        PostId postId,
        GenerateReplyRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken) =>
        TypedResults.ServerSentEvents(sender.CreateStream(
            request.ToGenerateReply(postId, worldId), cancellationToken));

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
        return TypedResults.Ok(new PageResponse<FeedItemResponse>(response, result.NextCursor));
    }

    [HttpGet("mine", Name = "GetChannelPosts")]
    [ActionName("GetChannelPosts")]
    public async Task<Ok<PageResponse<ChannelPostResponse>>> GetChannelPostsAsync(
        [FromQuery] WorldPostsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            request.ToQuery(),
            cancellationToken);
        IReadOnlyList<ChannelPostResponse> response = [.. result.Items.Select(PostResponseMapper.MapChannelPost)];
        return TypedResults.Ok(new PageResponse<ChannelPostResponse>(response, result.NextCursor));
    }

    [HttpGet("{postId}", Name = "GetPost")]
    [ActionName("GetPost")]
    public async Task<Results<Ok<PostDetailResponse>, NoContent>> GetPostAsync(
        PostId postId,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PostDetailQuery(postId, worldId),
            cancellationToken);
        return result is null ? TypedResults.NoContent() : TypedResults.Ok(result);
    }

    [HttpDelete("{postId}", Name = "DeletePost")]
    public async Task<NoContent> DeletePostAsync(
        PostId postId,
        [FromServices] IPostRepository posts,
        CancellationToken cancellationToken)
    {
        await posts.DeletePostAsync(postId, cancellationToken);
        return TypedResults.NoContent();
    }
}
