using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
    ITelegramUserAccessor telegramUser) : ControllerBase
{
    [HttpPost("posts/publications", Name = "GeneratePublication")]
    [ActionName("GeneratePublication")]
    public ServerSentEventsResult<PublicationStatusLiveEvent> GeneratePublication(
        StartPublicationRequest request,
        CancellationToken cancellationToken)
    {
        IAsyncEnumerable<PublicationStatusLiveEvent> result = sender.CreateStream(
            new GeneratePublicationCommand(
                request.PublicationId,
                TelegramUserId.From(telegramUser.User.Id),
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
        PublicationStatusResponse result = await sender.Send(
            new GetPublicationQuery(TelegramUserId.From(telegramUser.User.Id), publicationId),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPost("posts/publications/{publicationId}/moderation", Name = "ModeratePublication")]
    [ActionName("ModeratePublication")]
    public ServerSentEventsResult<LiveEvent> ModeratePublication(
        string publicationId,
        CancellationToken cancellationToken)
    {
        IAsyncEnumerable<LiveEvent> result = sender.CreateStream(
            new PublishPostCommand(
                TelegramUserId.From(telegramUser.User.Id),
                publicationId),
            cancellationToken);
        return TypedResults.ServerSentEvents(result);
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
        IReadOnlyList<FeedItemResponse> result = await sender.Send(
            new GetFeedQuery(TelegramUserId.From(telegramUser.User.Id)),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}
