using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Requests.Player;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Application.Player.DeletePlayerCommand;
using Reki.Application.Player.ExchangeEnergyCommand;
using Reki.Application.Player.GetReferral;
using Reki.Application.Player.MergePlayersCommand;
using Reki.Application.Player.RefreshDailyChallengeCommand;
using Reki.Application.Player.RegisterWebPushCommand;
using Reki.Application.Player.Responses;
using Reki.Application.Player.Streaks.GetStreak;
using Reki.Application.Player.Streaks.UpdateStreakCommand;
using Reki.Application.Player.UpdateLastSeenAtCommand;
using Reki.Application.RekiContext;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Produces("application/json")]
[Route("/")]
public sealed class PlayerController(
    IRekiContext rekiContext,
    IInvitationRepository invitations,
    ISender sender) : ControllerBase
{
    [HttpGet("me", Name = "GetCurrentPlayer")]
    [ActionName("GetCurrentPlayer")]
    public async Task<Ok<PlayerResponse>> GetCurrentPlayerAsync(CancellationToken cancellationToken)
    {
        var player = await rekiContext.Player;
        await sender.Send(new UpdateStreak(player), cancellationToken);
        await sender.Send(new RefreshDailyChallenge(player), cancellationToken);
        await sender.Send(new UpdateLastSeenAt(player), cancellationToken);
        return TypedResults.Ok(player.ToPlayerResponse());
    }

    [HttpGet("me/streak", Name = "GetPlayerStreak")]
    public async Task<Ok<PlayerStreakResponse>> GetPlayerStreakAsync(CancellationToken cancellationToken) =>
        TypedResults.Ok((await sender.Send(new PlayerStreakQuery(rekiContext.PlayerId), cancellationToken)).ToPlayerStreakResponse());

    [HttpPost("me/energy/exchange", Name = "ExchangeEnergy")]
    public async Task<Ok<EnergyExchangeResponse>> ExchangeEnergyAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ExchangeEnergy(), cancellationToken);
        return TypedResults.Ok(result);
    }

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

    [HttpPut("me/notification-channels/web-push", Name = "RegisterWebPush")]
    public async Task<Ok> RegisterWebPushAsync(WebPushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RegisterWebPush(request.Endpoint, request.P256dh, request.Auth), cancellationToken);
        return TypedResults.Ok();
    }

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

    [HttpPost("merge", Name = "MergePlayers")]
    [ActionName("MergePlayers")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<Ok> MergePlayersAsync(MergePlayersRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new MergePlayers(request.FromPlayerId, rekiContext.PlayerId), cancellationToken);
        return TypedResults.Ok();
    }

    [HttpDelete("me", Name = "DeleteCurrentPlayer")]
    [ActionName("DeleteCurrentPlayer")]
    public async Task<Ok> DeleteCurrentPlayerAsync(
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeletePlayer(rekiContext.PlayerId),
            cancellationToken);
        return TypedResults.Ok();
    }
}
