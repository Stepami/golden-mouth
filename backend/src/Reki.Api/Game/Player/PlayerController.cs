using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Game;
using Reki.Contracts.Game;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Game;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PlayerController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    GameMapper mapper) : ControllerBase
{
    [HttpGet("me", Name = "GetCurrentPlayer")]
    [ActionName("GetCurrentPlayer")]
    public async Task<Ok<PlayerResponse>> GetCurrentPlayerAsync(CancellationToken cancellationToken)
    {
        PlayerView result = await sender.Send(
            new GetPlayerQuery(telegramUser.User.Id),
            cancellationToken);
        return TypedResults.Ok(mapper.MapPlayer(result));
    }

    [HttpPost("onboarding", Name = "CompleteOnboarding")]
    [ActionName("CompleteOnboarding")]
    public async Task<Ok<PlayerResponse>> CompleteOnboardingAsync(
        OnboardingRequest request,
        CancellationToken cancellationToken)
    {
        PlayerView result = await sender.Send(
            new OnboardPlayerCommand(
                telegramUser.User.Id,
                ToStorageValue(request.BirthGroup),
                request.World.ToString().ToLowerInvariant(),
                request.Role,
                request.Avatar,
                request.Name,
                request.Handle,
                request.Address.ToString().ToLowerInvariant(),
                request.Star),
            cancellationToken);
        return TypedResults.Ok(mapper.MapPlayer(result));
    }

    [HttpPatch("settings", Name = "UpdateSettings")]
    [ActionName("UpdateSettings")]
    public async Task<Ok<OkResponse>> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateSettingsCommand(
                telegramUser.User.Id,
                request.Drama?.ToString().ToLowerInvariant(),
                request.Notifications,
                request.Quiet,
                request.Voice,
                request.Plus),
            cancellationToken);
        return TypedResults.Ok(new OkResponse(true));
    }

    [HttpPost("plus-interest", Name = "ClaimPlusInterest")]
    [ActionName("ClaimPlusInterest")]
    public async Task<Ok<PlusInterestResponse>> ClaimPlusInterestAsync(
        CancellationToken cancellationToken)
    {
        int bonus = await sender.Send(
            new ClaimPlusInterestCommand(telegramUser.User.Id),
            cancellationToken);
        return TypedResults.Ok(new PlusInterestResponse(bonus));
    }

    [HttpDelete("me", Name = "DeleteCurrentPlayer")]
    [ActionName("DeleteCurrentPlayer")]
    public async Task<Ok<OkResponse>> DeleteCurrentPlayerAsync(
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeletePlayerCommand(telegramUser.User.Id), cancellationToken);
        return TypedResults.Ok(new OkResponse(true));
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
