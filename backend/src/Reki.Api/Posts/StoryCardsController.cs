using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Common;
using Reki.Application.Posts;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Api.Posts;

[ApiController]
[Route("cards/story")]
public sealed class StoryCardsController(
    ISender sender,
    IStoryCardRenderer renderer) : ControllerBase
{
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

        var card = await sender.Send(
            new GetPublicStoryCardQuery(PlayerId.From(playerId), PostId.From(postId)),
            cancellationToken);
        Response.ContentType = "image/png";
        Response.Headers.CacheControl = "no-store";
        await renderer.RenderAsync(card, Response.Body, cancellationToken);
        return TypedResults.Empty;
    }
}
