using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Worlds;
using Reki.Api.Telemetry;
using Reki.Application.RekiContext;
using Reki.Application.Worlds;
using Reki.Application.Worlds.DeleteWorldCommand;
using Reki.Application.Worlds.GetWorldTemplates;
using Reki.Application.Worlds.Responses;
using Reki.Domain.Worlds;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("worlds")]
public sealed class WorldsController(
    ISender sender,
    IRekiContext rekiContext) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("templates", Name = "GetWorldTemplates")]
    [ActionName("GetWorldTemplates")]
    public async Task<Ok<IReadOnlyList<WorldResponse>>> GetWorldTemplatesAsync(CancellationToken cancellationToken)
    {
        var worlds = await sender.Send(new WorldTemplatesQuery(), cancellationToken);
        return TypedResults.Ok<IReadOnlyList<WorldResponse>>([.. worlds.Select(world => world.ToResponse())]);
    }

    [HttpGet(Name = "GetOwnedWorlds")]
    [ActionName("GetOwnedWorlds")]
    public async Task<Ok<IReadOnlyList<WorldResponse>>> GetOwnedWorldsAsync(CancellationToken cancellationToken)
    {
        var worlds = await rekiContext.Worlds;
        return TypedResults.Ok<IReadOnlyList<WorldResponse>>([.. worlds.Values.Select(world => world.ToResponse())]);
    }

    [HttpPost(Name = "CreateWorld")]
    [ActionName("CreateWorld")]
    public async Task<Ok<WorldResponse>> CreateWorldAsync(CreateWorldRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request.ToCreateWorld(), cancellationToken);
        ProductGoalHeaders.Write(Response, result.Goals);
        return TypedResults.Ok(result.Result.ToResponse());
    }

    [HttpPut("{worldId}", Name = "UpdateWorld")]
    [ActionName("UpdateWorld")]
    public async Task<Ok> UpdateWorldAsync(
        CreateWorldRequest request,
        WorldId worldId,
        CancellationToken cancellationToken)
    {
        var command = request.ToUpdateWorld(worldId);
        await sender.Send(command, cancellationToken);
        return TypedResults.Ok();
    }

    [HttpDelete("{worldId}", Name = "DeleteWorld")]
    public async Task<NoContent> DeleteWorldAsync(WorldId worldId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWorld(rekiContext.PlayerId, worldId), cancellationToken);
        return TypedResults.NoContent();
    }
}
