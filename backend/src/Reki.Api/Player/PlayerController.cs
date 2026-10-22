using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Application.Common;
using Reki.Application.Invites;
using Reki.Application.Player;
using Reki.Contracts.Player;
using Reki.Domain.Player;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Player;

[ApiController]
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
        [FromServices] ITelegramUserAccessor telegramUser,
        CancellationToken cancellationToken)
    {
        var player = await playerRepository.GetPlayerAsync(TelegramUserId.From(telegramUser.User.Id), cancellationToken);
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
        [FromServices] ITelegramUserAccessor telegramUser,
        CancellationToken cancellationToken)
    {
        var player = await sender.Send(
            new OnboardPlayer(
                TelegramUserId.From(telegramUser.User.Id),
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
            new UpdateSettings(
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
            new ClaimPlusInterest(request?.Interest ?? true),
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
