using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Game.Common;

namespace Reki.Api.Game.Posts;

[ApiController]
[Route("cards/story")]
public sealed class StoryCardsController(IStoryCardRenderer renderer) : ControllerBase
{
    [HttpGet("{postId}.png", Name = "GetStoryCard")]
    [ActionName("GetStoryCard")]
    public async Task<EmptyHttpResult> GetStoryCardAsync(
        string postId,
        CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "image/png";
        await renderer.RenderAsync(postId, Response.Body, cancellationToken);
        return TypedResults.Empty;
    }
}
