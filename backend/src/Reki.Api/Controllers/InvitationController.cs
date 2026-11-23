using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Invites;
using Reki.Application.Invites.GetReferral;
using Reki.Application.Invites.Responses;
using Reki.Application.Player.Responses;
using Reki.Application.RekiContext;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("/")]
public sealed class InvitationController(
    IRekiContext rekiContext,
    IInvitationRepository invitations,
    ISender sender) : ControllerBase
{
    [HttpGet("invitation", Name = "GetInvitation")]
    [ActionName("GetInvitation")]
    public async Task<Ok<InvitationResponse>> GetInvitationAsync(CancellationToken cancellationToken)
    {
        var invitation = await invitations.GetPendingInviteAsync(rekiContext.PlayerId, cancellationToken);
        return TypedResults.Ok(new InvitationResponse(invitation?.BonusAmount ?? 0));
    }

    [HttpGet("referral", Name = "GetReferralCode")]
    [ActionName("GetReferralCode")]
    public async Task<Ok<ReferralResponse>> GetReferralCodeAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReferralQuery(rekiContext.PlayerId),
            cancellationToken);
        return TypedResults.Ok(result);
    }
}