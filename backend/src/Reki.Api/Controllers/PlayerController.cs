using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Requests.Player;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Application.Player.DeletePlayerCommand;
using Reki.Application.Player.GetReferral;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("/")]
public sealed class PlayerController(
    IRekiContext rekiContext,
    IInvitationRepository invitationRepository,
    IPlayerRepository playerRepository,
    ISender sender) : ControllerBase
{
    [HttpGet("me", Name = "GetCurrentPlayer")]
    [ActionName("GetCurrentPlayer")]
    public async Task<Results<Ok<PlayerResponse>, NotFound<ProblemDetails>>> GetCurrentPlayerAsync(
        CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetPlayerAsync(rekiContext.TelegramUserId, cancellationToken);
        if (player is null)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Type = "urn:reki:problem:resource-not-found",
                Detail = "The requested resource was not found.",
                Instance = Request.Path,
            };
            problemDetails.Extensions["failure"] = null;
            return TypedResults.NotFound(problemDetails);
        }

        return TypedResults.Ok(player.ToResponse());
    }

    [HttpPost("onboarding", Name = "CompleteOnboarding")]
    [ActionName("CompleteOnboarding")]
    public async Task<Ok<PlayerResponse>> CompleteOnboardingAsync(
        OnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(
            request.ToOnboardPlayer(rekiContext.TelegramUserId),
            cancellationToken);
        return TypedResults.Ok(player.ToResponse());
    }

    [HttpGet("invitation", Name = "GetInvitation")]
    [ActionName("GetInvitation")]
    public async Task<Ok<InvitationResponse>> GetInvitationAsync(CancellationToken cancellationToken)
    {
        var player = await rekiContext.Player;
        var invitation = await invitationRepository.GetPendingInviteAsync(player.Id, cancellationToken);
        return TypedResults.Ok(new InvitationResponse(invitation?.BonusAmount ?? 0));
    }

    [HttpGet("referral", Name = "GetReferralCode")]
    [ActionName("GetReferralCode")]
    public async Task<Ok<ReferralResponse>> GetReferralCodeAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReferralQuery(),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpPatch("settings", Name = "UpdateSettings")]
    [ActionName("UpdateSettings")]
    public async Task<Ok> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToUpdateSettings(),
            cancellationToken);
        return TypedResults.Ok();
    }

    [HttpPost("plus-interest", Name = "ClaimPlusInterest")]
    [ActionName("ClaimPlusInterest")]
    public async Task<Ok<PlusInterestResponse>> ClaimPlusInterestAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PlusInterestRequest? request,
        CancellationToken cancellationToken)
    {
        var bonus = await sender.Send(
            request.ToClaimPlusInterest(),
            cancellationToken);
        return TypedResults.Ok(new PlusInterestResponse(bonus));
    }

    [HttpDelete("me", Name = "DeleteCurrentPlayer")]
    [ActionName("DeleteCurrentPlayer")]
    public async Task<Ok> DeleteCurrentPlayerAsync(
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeletePlayer(),
            cancellationToken);
        return TypedResults.Ok();
    }
}
