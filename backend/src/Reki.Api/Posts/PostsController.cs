using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Posts;
using Reki.Contracts.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Posts;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PostsController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    ApiMapper mapper,
    PostLiveEventMapper liveEvents) : ControllerBase
{
    [HttpPost("posts", Name = "PublishPost")]
    [ActionName("PublishPost")]
    public async Task<ServerSentEventsResult<object>> PublishPostAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        PublishedPost published = await sender.Send(
            new PublishPostCommand(
                TelegramUserId.From(telegramUser.User.Id),
                request.Text,
                request.OwnWords,
                request.WantId,
                null),
            cancellationToken);
        return TypedResults.ServerSentEvents(liveEvents.MapPublishedAsync(published, cancellationToken));
    }

    [HttpPost("posts/publications", Name = "GeneratePublication")]
    [ActionName("GeneratePublication")]
    public ServerSentEventsResult<PublicationStatusLiveEvent> GeneratePublication(
        StartPublicationRequest request,
        CancellationToken cancellationToken)
    {
        IAsyncEnumerable<PublicationStatus> result = sender.CreateStream(
            new GeneratePublicationCommand(
                request.PublicationId,
                TelegramUserId.From(telegramUser.User.Id),
                request.Text,
                request.OwnWords,
                request.WantId),
            cancellationToken);
        return TypedResults.ServerSentEvents(
            PostLiveEventMapper.MapStatusesAsync(result, request.PublicationId, cancellationToken));
    }

    [HttpGet("posts/publications/{publicationId}", Name = "GetPublicationStatus")]
    [ActionName("GetPublicationStatus")]
    public async Task<Ok<PublicationStatusResponse>> GetPublicationStatusAsync(
        string publicationId,
        CancellationToken cancellationToken)
    {
        Publication publication = await sender.Send(
            new GetPublicationQuery(TelegramUserId.From(telegramUser.User.Id), publicationId),
            cancellationToken);
        PostResultResponse? result = publication.Result is null
            ? null
            : mapper.MapPublishedPost(publication.Result);
        return TypedResults.Ok(new PublicationStatusResponse(
            publication.Id,
            PostLiveEventMapper.MapPublicationStatus(publication.Status),
            result));
    }

    [HttpPost("posts/publications/{publicationId}/moderation", Name = "ModeratePublication")]
    [ActionName("ModeratePublication")]
    public async Task<ServerSentEventsResult<object>> ModeratePublicationAsync(
        string publicationId,
        CancellationToken cancellationToken)
    {
        PublishedPost published = await sender.Send(
            PublishPostCommand.ForPublication(
                TelegramUserId.From(telegramUser.User.Id),
                publicationId),
            cancellationToken);
        return TypedResults.ServerSentEvents(liveEvents.MapPublishedAsync(published, cancellationToken));
    }

    [HttpPost("posts/{postId}/replies", Name = "ReplyToPost")]
    [ActionName("ReplyToPost")]
    public async Task<Ok> ReplyToPostAsync(
        string postId,
        ReplyRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new AddReplyCommand(
                TelegramUserId.From(telegramUser.User.Id),
                PostId.From(postId),
                request.Text),
            cancellationToken);
        return TypedResults.Ok();
    }

    [HttpGet("feed", Name = "GetFeed")]
    [ActionName("GetFeed")]
    public async Task<Ok<IReadOnlyList<FeedItemResponse>>> GetFeedAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<GamePost> posts = await sender.Send(
            new GetFeedQuery(TelegramUserId.From(telegramUser.User.Id)),
            cancellationToken);
        return TypedResults.Ok<IReadOnlyList<FeedItemResponse>>(
            posts.Select(mapper.MapFeedItem).ToArray());
    }
}
