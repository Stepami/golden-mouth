using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Player;
using Reki.Application.Player;
using Reki.Application.Player.DeletePlayerCommand;
using Reki.Application.Player.GetBoosterStock;
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
[Route("me")]
public sealed class PlayerController(
    IRekiContext rekiContext,
    ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetCurrentPlayer")]
    [ActionName("GetCurrentPlayer")]
    public async Task<Ok<PlayerResponse>> GetCurrentPlayerAsync(CancellationToken cancellationToken)
    {
        var player = await rekiContext.Player;
        await sender.Send(new UpdateStreak(player), cancellationToken);
        await sender.Send(new RefreshDailyChallenge(player), cancellationToken);
        await sender.Send(new UpdateLastSeenAt(player), cancellationToken);
        var stock = await sender.Send(new BoosterStockQuery(player.Id), cancellationToken);
        return TypedResults.Ok(player.ToPlayerResponse() with { BoosterStock = stock });
    }

    [HttpGet("streak", Name = "GetPlayerStreak")]
    public async Task<Ok<PlayerStreakResponse>> GetPlayerStreakAsync(CancellationToken cancellationToken) =>
        TypedResults.Ok((await sender.Send(new PlayerStreakQuery(rekiContext.PlayerId), cancellationToken)).ToPlayerStreakResponse());

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

    [HttpPut("notification-channels/web-push", Name = "RegisterWebPush")]
    public async Task<Ok> RegisterWebPushAsync(WebPushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RegisterWebPush(request.Endpoint, request.P256dh, request.Auth), cancellationToken);
        return TypedResults.Ok();
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

    [HttpGet("referral", Name = "GetReferralCode")]
    [ActionName("GetReferralCode")]
    public async Task<Ok<ReferralResponse>> GetReferralCodeAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReferralQuery(rekiContext.PlayerId),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    [HttpDelete(Name = "DeleteCurrentPlayer")]
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
