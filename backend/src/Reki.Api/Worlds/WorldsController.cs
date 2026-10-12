using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Worlds;
using Reki.Contracts.Worlds;
using Reki.Domain.Worlds;

namespace Reki.Api.Worlds;

[ApiController]
[Produces("application/json")]
[Route("worlds")]
public sealed class WorldsController(
    ISender sender,
    ApiMapper mapper) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("templates", Name = "GetWorldTemplates")]
    [ActionName("GetWorldTemplates")]
    public async Task<Ok<IReadOnlyList<WorldResponse>>> GetWorldTemplatesAsync(CancellationToken cancellationToken)
    {
        var worlds = await sender.Send(new GetWorldTemplatesQuery(), cancellationToken);
        return TypedResults.Ok<IReadOnlyList<WorldResponse>>(worlds.Select(mapper.MapWorld).ToArray());
    }

    [HttpGet(Name = "GetOwnedWorlds")]
    [ActionName("GetOwnedWorlds")]
    public async Task<Ok<IReadOnlyList<WorldResponse>>> GetOwnedWorldsAsync(CancellationToken cancellationToken)
    {
        var worlds = await sender.Send(new GetOwnedWorldsQuery(), cancellationToken);
        return TypedResults.Ok<IReadOnlyList<WorldResponse>>(worlds.Select(mapper.MapWorld).ToArray());
    }

    [HttpPost(Name = "CreateWorld")]
    [ActionName("CreateWorld")]
    public async Task<Ok<WorldResponse>> CreateWorldAsync(CreateWorldRequest request, CancellationToken cancellationToken)
    {
        var world = await sender.Send(
            new CreateWorldCommand(mapper.MapWorldDefinition(request)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapWorld(world));
    }

    [HttpPatch("{worldId}", Name = "UpdateWorld")]
    [ActionName("UpdateWorld")]
    public async Task<Ok> UpdateWorldAsync(
        UpdateWorldRequest request,
        string worldId,
        CancellationToken cancellationToken)
    {
        var command = mapper.MapWorldUpdateRequest(
            request, WorldId.From(worldId));
        await sender.Send(command, cancellationToken);
        return TypedResults.Ok();
    }
}