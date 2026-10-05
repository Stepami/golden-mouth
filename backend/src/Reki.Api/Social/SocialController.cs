using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Social;
using Reki.Contracts.Social;
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
            new GetFriendsQuery(telegramUser.User.Id),
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
            new GetDirectMessagesQuery(telegramUser.User.Id, starId),
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
            new SendDirectMessageCommand(telegramUser.User.Id, starId, request.Text),
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
            new ForgetStarMemoryCommand(telegramUser.User.Id, starId, request.Fact),
            cancellationToken);
        return TypedResults.Ok();
    }

    [HttpGet("challenges/active", Name = "GetActiveChallenge")]
    [ActionName("GetActiveChallenge")]
    public async Task<Ok<ChallengeResponse?>> GetActiveChallengeAsync(
        CancellationToken cancellationToken)
    {
        GameChallenge? challenge = await sender.Send(
            new GetActiveChallengeQuery(telegramUser.User.Id),
            cancellationToken);
        ChallengeResponse? response = challenge is null
            ? null
            : new ChallengeResponse(
                challenge.Id,
                "я",
                new FriendResponse(
                    challenge.FriendId,
                    "друг",
                    "pick0",
                    "Сцена",
                    0,
                    string.Empty,
                    "его"),
                challenge.Day,
                challenge.Days,
                challenge.MyScore,
                challenge.RivalScore,
                challenge.BestPost);
        return TypedResults.Ok<ChallengeResponse?>(response);
    }

    [HttpPost("challenges", Name = "StartChallenge")]
    [ActionName("StartChallenge")]
    public async Task<Ok> StartChallengeAsync(
        StartChallengeRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new StartChallengeCommand(telegramUser.User.Id, request.FriendId),
            cancellationToken);
        return TypedResults.Ok();
    }
}
