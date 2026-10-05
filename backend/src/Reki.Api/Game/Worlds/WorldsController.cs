using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Game.Common;
using Reki.Application.Game.Worlds;
using Reki.Contracts.Game.Common;
using Reki.Contracts.Game.Player;
using Reki.Contracts.Game.Worlds;
using Reki.Domain.Game.Worlds;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Game.Worlds;

[ApiController]
[Produces("application/json")]
[Route("worlds")]
public sealed class WorldsController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    GameMapper mapper) : ControllerBase
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
    public async Task<Ok<OkResponse>> SaveOwnWorldAsync(
        UpdateWorldRulesRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SaveWorldRulesCommand(telegramUser.User.Id, mapper.MapWorldRulesRequest(request)),
            cancellationToken);
        return TypedResults.Ok(new OkResponse(true));
    }
}
