using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Player;
using Reki.Contracts.Player;

namespace Reki.Api.Player;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PlayerController(
    ISender sender,
    IGameStore store,
    IOptions<GameBalance> balance) : ControllerBase
{
    [HttpGet("me", Name = "GetCurrentPlayer")]
    [ActionName("GetCurrentPlayer")]
    public async Task<Ok<PlayerResponse>> GetCurrentPlayerAsync(CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        return TypedResults.Ok(player.ToResponse());
    }

    [HttpPost("onboarding", Name = "CompleteOnboarding")]
    [ActionName("CompleteOnboarding")]
    public async Task<Ok<PlayerResponse>> CompleteOnboardingAsync(
        OnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(
            new OnboardPlayerCommand(
                ToStorageValue(request.BirthGroup),
                request.Address.ToString().ToLowerInvariant(),
                request.ReferralCode,
                request.AgeBand),
            cancellationToken);
        return TypedResults.Ok(player.ToResponse());
    }

    [HttpGet("invitation", Name = "GetInvitation")]
    [ActionName("GetInvitation")]
    public async Task<Ok<InvitationResponse>> GetInvitationAsync(CancellationToken cancellationToken)
    {
        var player = await sender.Send(new GetPlayerQuery(), cancellationToken);
        var invitation = await store.GetPendingInviteAsync(player.Id, cancellationToken);
        return TypedResults.Ok(new InvitationResponse(invitation is null ? 0 : balance.Value.InviteBonusBoth));
    }

    [HttpGet("referral", Name = "GetReferralCode")]
    [ActionName("GetReferralCode")]
    public async Task<Ok<ReferralResponse>> GetReferralCodeAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetReferralQuery(),
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
            new UpdateSettingsCommand(
                request.Notifications,
                request.Quiet,
                request.Plus,
                request.Address?.ToString().ToLowerInvariant()),
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
            new ClaimPlusInterestCommand(request?.Interest ?? true),
            cancellationToken);
        return TypedResults.Ok(new PlusInterestResponse(bonus));
    }

    [HttpDelete("me", Name = "DeleteCurrentPlayer")]
    [ActionName("DeleteCurrentPlayer")]
    public async Task<Ok> DeleteCurrentPlayerAsync(
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeletePlayerCommand(),
            cancellationToken);
        return TypedResults.Ok();
    }

    private static string ToStorageValue(BirthGroup birthGroup)
    {
        return birthGroup switch
        {
            BirthGroup.Teen => "14-17",
            BirthGroup.Adult => "18+",
            _ => throw new ArgumentOutOfRangeException(nameof(birthGroup)),
        };
    }
}
