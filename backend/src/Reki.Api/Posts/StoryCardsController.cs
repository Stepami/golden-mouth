using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Posts;

[ApiController]
[Route("cards/story")]
public sealed class StoryCardsController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    IStoryCardRenderer renderer) : ControllerBase
{
    [HttpGet("{postId}.png", Name = "GetStoryCard")]
    [ActionName("GetStoryCard")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/png")]
    public async Task<EmptyHttpResult> GetStoryCardAsync(
        string postId,
        CancellationToken cancellationToken)
    {
        StoryCardData card = await sender.Send(
            new GetStoryCardQuery(
                TelegramUserId.From(telegramUser.User.Id),
                PostId.From(postId)),
            cancellationToken);
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "image/png";
        Response.Headers.CacheControl = "private, max-age=300";
        Response.Headers.ETag = $"\"story-v1-{card.PostId}\"";
        await renderer.RenderAsync(card, Response.Body, cancellationToken);
        return TypedResults.Empty;
    }

    [AllowAnonymous]
    [HttpGet("{playerId}/{postId}.png", Name = "GetPublicStoryCard")]
    [ActionName("GetPublicStoryCard")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/png")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPublicStoryCardAsync(
        string playerId,
        string postId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParseExact(playerId, "N", out _) ||
            !Guid.TryParseExact(postId, "N", out _))
        {
            return TypedResults.NotFound();
        }

        StoryCardData card = await sender.Send(
            new GetPublicStoryCardQuery(PlayerId.From(playerId), PostId.From(postId)),
            cancellationToken);
        Response.ContentType = "image/png";
        Response.Headers.CacheControl = "no-store";
        await renderer.RenderAsync(card, Response.Body, cancellationToken);
        return TypedResults.Empty;
    }
}
