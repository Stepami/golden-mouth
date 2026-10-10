using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Worlds;
using Reki.Contracts.Worlds;
using Reki.Domain.Worlds;
using ContractWorldId = Reki.Contracts.Player.WorldId;

namespace Reki.Api.Worlds;

[ApiController]
[Produces("application/json")]
[Route("worlds")]
public sealed class WorldsController(
    ISender sender,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("{worldId}/rules", Name = "GetWorldRules")]
    [ActionName("GetWorldRules")]
    public async Task<Ok<WorldRulesResponse>> GetWorldRulesAsync(
        ContractWorldId worldId,
        CancellationToken cancellationToken)
    {
        WorldRules result = await sender.Send(
            new GetWorldRulesQuery(WorldId.From(worldId.ToString())),
            cancellationToken);
        return TypedResults.Ok(mapper.MapWorldRules(result));
    }

    [HttpPut("own", Name = "SaveOwnWorld")]
    [ActionName("SaveOwnWorld")]
    public async Task<Ok> SaveOwnWorldAsync(
        UpdateWorldRulesRequest request,
        CancellationToken cancellationToken)
    {
        SaveWorldRulesCommand command = mapper.MapWorldRulesRequest(
            request);
        await sender.Send(command, cancellationToken);
        return TypedResults.Ok();
    }
}
