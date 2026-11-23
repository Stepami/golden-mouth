using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Posts.GetPublicStoryCard;
using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Api.Controllers;

[ApiController]
[Route("cards/story")]
public sealed class StoryCardsController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{playerId}/{postId}.png", Name = "GetPublicStoryCard")]
    [ActionName("GetPublicStoryCard")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/png")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPublicStoryCardAsync(
        PlayerId playerId,
        PostId postId,
        CancellationToken cancellationToken)
    {
        var png = await sender.Send(
            new PublicStoryCardQuery(playerId, postId),
            cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return TypedResults.Stream(png, "image/png");
    }
}
