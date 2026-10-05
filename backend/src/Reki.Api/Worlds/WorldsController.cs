using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Worlds;
using Reki.Contracts.Player;
using Reki.Contracts.Worlds;
using Reki.Domain.Worlds;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Worlds;

[ApiController]
[Produces("application/json")]
[Route("worlds")]
public sealed class WorldsController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("{worldId}/rules", Name = "GetWorldRules")]
    [ActionName("GetWorldRules")]
    public async Task<Ok<WorldRulesResponse>> GetWorldRulesAsync(
        WorldId worldId,
        CancellationToken cancellationToken)
    {
        WorldRules result = await sender.Send(
            new GetWorldRulesQuery(telegramUser.User.Id, worldId.ToString().ToLowerInvariant()),
            cancellationToken);
        return TypedResults.Ok(mapper.MapWorldRules(result));
    }

    [HttpPut("own", Name = "SaveOwnWorld")]
    [ActionName("SaveOwnWorld")]
    public async Task<Ok> SaveOwnWorldAsync(
        UpdateWorldRulesRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SaveWorldRulesCommand(telegramUser.User.Id, mapper.MapWorldRulesRequest(request)),
            cancellationToken);
        return TypedResults.Ok();
    }
}
