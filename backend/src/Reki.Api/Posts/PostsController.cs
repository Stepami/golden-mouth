using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Api.Serialization;
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
    ApiMapper mapper) : ControllerBase
{
    [HttpPost("posts", Name = "PublishPost")]
    [ActionName("PublishPost")]
    public async Task<EmptyHttpResult> PublishPostAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        PublishedPost published = await sender.Send(
            new PublishPostCommand(
                TelegramUserId.From(telegramUser.User.Id),
                request.Text,
                request.OwnWords,
                request.WantId),
            cancellationToken);
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";

        await WriteEventAsync(
            new ViewsLiveEvent(LiveEventType.Views, Math.Min(48, published.Post.Views)),
            ApiJsonSerializerContext.Default.ViewsLiveEvent,
            cancellationToken);
        await WriteEventAsync(
            new ReactionLiveEvent(LiveEventType.React, published.Reaction),
            ApiJsonSerializerContext.Default.ReactionLiveEvent,
            cancellationToken);
        GameComment comment = published.Post.Comments[0];
        await WriteEventAsync(
            new CommentLiveEvent(LiveEventType.Comment, mapper.MapComment(comment)),
            ApiJsonSerializerContext.Default.CommentLiveEvent,
            cancellationToken);
        await WriteEventAsync(
            new TypingLiveEvent(LiveEventType.Typing, published.StarName),
            ApiJsonSerializerContext.Default.TypingLiveEvent,
            cancellationToken);
        await WriteEventAsync(
            new ResultLiveEvent(LiveEventType.Result, mapper.MapPublishedPost(published)),
            ApiJsonSerializerContext.Default.ResultLiveEvent,
            cancellationToken);

        return TypedResults.Empty;
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

    private async Task WriteEventAsync<T>(
        T gameEvent,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(gameEvent, jsonTypeInfo);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
