using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Requests.Player;
using Reki.Application.Player.ExchangeEnergyCommand;
using Reki.Application.Player.Responses;
using Reki.Application.Posts.ClaimOnboardingBonusCommand;
using Reki.Application.RekiContext;
using Reki.Domain.Posts;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("bonus")]
public sealed class BonusController(ISender sender) : ControllerBase
{
    [HttpPost("plus-interest", Name = "ClaimPlusInterest")]
    [ActionName("ClaimPlusInterest")]
    public async Task<Ok<PlusInterestResponse>> ClaimPlusInterestAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]
        PlusInterestRequest? request,
        CancellationToken cancellationToken)
    {
        var bonus = await sender.Send(
            request.ToClaimPlusInterest(),
            cancellationToken);
        return TypedResults.Ok(new PlusInterestResponse(bonus));
    }

    [HttpPost("exchange-energy", Name = "ExchangeEnergy")]
    public async Task<Ok<EnergyExchangeResponse>> ExchangeEnergyAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExchangeEnergy(), cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpGet("onboarding/{postId}", Name = "ClaimOnboardingBonus")]
    [ActionName("ClaimOnboardingBonus")]
    public async Task<Ok<bool>> ClaimOnboardingBonusAsync(
        PostId postId,
        [FromServices] IRekiContext rekiContext,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(
            new ClaimOnboardingBonus(await rekiContext.Player, postId), cancellationToken));
}