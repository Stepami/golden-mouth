using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.RekiContext;
using Reki.Application.Worlds.CreateWorldCommand;
using Reki.Application.Worlds.GetWorldTemplates;
using Reki.Application.Worlds.UpdateWorldCommand;
using Reki.Contracts.Worlds;
using Reki.Domain.Worlds;

namespace Reki.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("worlds")]
public sealed class WorldsController(
    ISender sender,
    IRekiContext rekiContext,
    IApiMapper mapper) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("templates", Name = "GetWorldTemplates")]
    [ActionName("GetWorldTemplates")]
    public async Task<Ok<IReadOnlyList<WorldResponse>>> GetWorldTemplatesAsync(CancellationToken cancellationToken)
    {
        var worlds = await sender.Send(new WorldTemplatesQuery(), cancellationToken);
        return TypedResults.Ok<IReadOnlyList<WorldResponse>>([.. worlds.Select(mapper.MapWorld)]);
    }

    [HttpGet(Name = "GetOwnedWorlds")]
    [ActionName("GetOwnedWorlds")]
    public async Task<Ok<IReadOnlyList<WorldResponse>>> GetOwnedWorldsAsync(CancellationToken cancellationToken)
    {
        var worlds = await rekiContext.Worlds;
        return TypedResults.Ok<IReadOnlyList<WorldResponse>>([.. worlds.Values.Select(mapper.MapWorld)]);
    }

    [HttpPost(Name = "CreateWorld")]
    [ActionName("CreateWorld")]
    public async Task<Ok<WorldResponse>> CreateWorldAsync(CreateWorldRequest request, CancellationToken cancellationToken)
    {
        var world = await sender.Send(
            new CreateWorld(mapper.MapWorldDefinition(request)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapWorld(world));
    }

    [HttpPut("{worldId}", Name = "UpdateWorld")]
    [ActionName("UpdateWorld")]
    public async Task<Ok> UpdateWorldAsync(
        CreateWorldRequest request,
        string worldId,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorld(WorldId.From(worldId), mapper.MapWorldDefinition(request));
        await sender.Send(command, cancellationToken);
        return TypedResults.Ok();
    }
}