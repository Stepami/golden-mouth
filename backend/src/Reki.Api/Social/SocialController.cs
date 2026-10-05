using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Social;
using Reki.Contracts.Social;
using Reki.Domain.Player;
using Reki.Domain.Social;
using TgMiniAppAuth.AuthContext.User;

namespace Reki.Api.Social;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class SocialController(
    ISender sender,
    ITelegramUserAccessor telegramUser,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("friends", Name = "GetFriends")]
    [ActionName("GetFriends")]
    public async Task<Ok<IReadOnlyList<FriendResponse>>> GetFriendsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FriendSummary> friends = await sender.Send(
            new GetFriendsQuery(TelegramUserId.From(telegramUser.User.Id)),
            cancellationToken);
        IReadOnlyList<FriendResponse> response = mapper.MapFriends(friends);
        return TypedResults.Ok(response);
    }

    [HttpGet("dm/{starId}", Name = "GetDirectMessages")]
    [ActionName("GetDirectMessages")]
    public async Task<Ok<DmResponse>> GetDirectMessagesAsync(
        string starId,
        CancellationToken cancellationToken)
    {
        DirectMessageView result = await sender.Send(
            new GetDirectMessagesQuery(
                TelegramUserId.From(telegramUser.User.Id),
                StarId.From(starId)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapDirectMessages(result, starId));
    }

    [HttpPost("dm/{starId}", Name = "SendDirectMessage")]
    [ActionName("SendDirectMessage")]
    public async Task<Ok<SendDirectMessageResponse>> SendDirectMessageAsync(
        string starId,
        DirectMessageRequest request,
        CancellationToken cancellationToken)
    {
        SentDirectMessage result = await sender.Send(
            new SendDirectMessageCommand(
                TelegramUserId.From(telegramUser.User.Id),
                StarId.From(starId),
                request.Text),
            cancellationToken);
        return TypedResults.Ok(mapper.MapSentDirectMessage(result));
    }

    [HttpDelete("stars/{starId}/memory", Name = "ForgetStarMemory")]
    [ActionName("ForgetStarMemory")]
    public async Task<Ok> ForgetStarMemoryAsync(
        string starId,
        ForgetMemoryRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ForgetStarMemoryCommand(
                TelegramUserId.From(telegramUser.User.Id),
                StarId.From(starId),
                request.Fact),
            cancellationToken);
        return TypedResults.Ok();
    }

    [HttpGet("challenges/active", Name = "GetActiveChallenge")]
    [ActionName("GetActiveChallenge")]
    public async Task<Ok<ChallengeResponse?>> GetActiveChallengeAsync(
        CancellationToken cancellationToken)
    {
        ChallengeView? view = await sender.Send(
            new GetActiveChallengeQuery(TelegramUserId.From(telegramUser.User.Id)),
            cancellationToken);
        ChallengeResponse? response = null;
        if (view is not null)
        {
            string rivalPronoun = view.Rival.Address == "she" ? "её" : "его";
            response = new ChallengeResponse(
                view.Challenge.Id.Value,
                "я",
                new FriendResponse(
                    view.Rival.Id.Value,
                    view.Rival.Name,
                    view.Rival.Avatar,
                    view.Rival.World.Value,
                    view.Rival.Subscribers,
                    string.Empty,
                    rivalPronoun),
                view.Day,
                view.Challenge.Days,
                view.Challenge.GetPerformance(view.PlayerId).Score,
                view.Challenge.GetPerformance(view.Rival.Id).Score,
                view.Challenge.BestPost);
        }

        return TypedResults.Ok<ChallengeResponse?>(response);
    }

    [HttpPost("challenges", Name = "StartChallenge")]
    [ActionName("StartChallenge")]
    public async Task<Ok> StartChallengeAsync(
        StartChallengeRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new StartChallengeCommand(
                TelegramUserId.From(telegramUser.User.Id),
                PlayerId.From(request.FriendId)),
            cancellationToken);
        return TypedResults.Ok();
    }
}
