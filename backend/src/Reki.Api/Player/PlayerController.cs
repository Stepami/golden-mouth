using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Player;
using Reki.Contracts.Player;
using Reki.Domain.Player;
using Reki.Domain.Social;
using TgMiniAppAuth.AuthContext.User;
using DomainWorldId = Reki.Domain.Worlds.WorldId;

namespace Reki.Api.Player;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class PlayerController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("me", Name = "GetCurrentPlayer")]
    [ActionName("GetCurrentPlayer")]
    public async Task<Ok<PlayerResponse>> GetCurrentPlayerAsync(CancellationToken cancellationToken)
    {
        PlayerView result = await sender.Send(
            new GetPlayerQuery(TelegramUserId.From(telegramUser.User.Id)),
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
                TelegramUserId.From(telegramUser.User.Id),
                ToStorageValue(request.BirthGroup),
                DomainWorldId.From(request.World.ToString()),
                request.Role,
                request.Name,
                request.Handle,
                request.Address.ToString().ToLowerInvariant(),
                StarId.From(request.Star)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapPlayer(result));
    }

    [HttpPatch("settings", Name = "UpdateSettings")]
    [ActionName("UpdateSettings")]
    public async Task<Ok> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new UpdateSettingsCommand(
                TelegramUserId.From(telegramUser.User.Id),
                request.Drama?.ToString().ToLowerInvariant(),
                request.Notifications,
                request.Quiet,
                request.Voice,
                request.Plus),
            cancellationToken);
        return TypedResults.Ok();
    }

    [HttpPost("plus-interest", Name = "ClaimPlusInterest")]
    [ActionName("ClaimPlusInterest")]
    public async Task<Ok<PlusInterestResponse>> ClaimPlusInterestAsync(
        CancellationToken cancellationToken)
    {
        int bonus = await sender.Send(
            new ClaimPlusInterestCommand(TelegramUserId.From(telegramUser.User.Id)),
            cancellationToken);
        return TypedResults.Ok(new PlusInterestResponse(bonus));
    }

    [HttpDelete("me", Name = "DeleteCurrentPlayer")]
    [ActionName("DeleteCurrentPlayer")]
    public async Task<Ok> DeleteCurrentPlayerAsync(
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeletePlayerCommand(TelegramUserId.From(telegramUser.User.Id)),
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
