using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Mapping;
using Reki.Application.Social.ForgetStarMemoryCommand;
using Reki.Application.Social.GetActiveChallenge;
using Reki.Application.Social.GetDirectMessages;
using Reki.Application.Social.GetFriends;
using Reki.Application.Social.SendDirectMessageCommand;
using Reki.Application.Social.StartChallengeCommand;
using Reki.Contracts.Social;
using Reki.Domain.Player;
using Reki.Domain.Social;

namespace Reki.Api.Social;

[ApiController]
[Produces("application/json")]
[Route("/")]
public sealed class SocialController(
    ISender sender,
    ApiMapper mapper) : ControllerBase
{
    [HttpGet("friends", Name = "GetFriends")]
    [ActionName("GetFriends")]
    public async Task<Ok<IReadOnlyList<FriendResponse>>> GetFriendsAsync(
        CancellationToken cancellationToken)
    {
        var friends = await sender.Send(
            new FriendsQuery(),
            cancellationToken);
        var response = mapper.MapFriends(friends);
        return TypedResults.Ok(response);
    }

    [HttpGet("dm/{starId}", Name = "GetDirectMessages")]
    [ActionName("GetDirectMessages")]
    public async Task<Ok<DmResponse>> GetDirectMessagesAsync(
        string starId,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DirectMessagesQuery(StarId.From(starId), Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapDirectMessages(result));
    }

    [HttpPost("dm/{starId}", Name = "SendDirectMessage")]
    [ActionName("SendDirectMessage")]
    public async Task<Ok<SendDirectMessageResponse>> SendDirectMessageAsync(
        string starId,
        DirectMessageRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SendDirectMessage(
                StarId.From(starId),
                request.Text,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok(mapper.MapSentDirectMessage(result));
    }

    [HttpDelete("stars/{starId}/memory", Name = "ForgetStarMemory")]
    [ActionName("ForgetStarMemory")]
    public async Task<Ok> ForgetStarMemoryAsync(
        string starId,
        ForgetMemoryRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ForgetStarMemory(
                StarId.From(starId),
                request.Fact,
                Domain.Worlds.WorldId.From(worldId)),
            cancellationToken);
        return TypedResults.Ok();
    }

    [HttpGet("challenges/active", Name = "GetActiveChallenge")]
    [ActionName("GetActiveChallenge")]
    public async Task<Ok<ChallengeResponse?>> GetActiveChallengeAsync(
        CancellationToken cancellationToken)
    {
        var view = await sender.Send(
            new ActiveChallengeQuery(),
            cancellationToken);
        ChallengeResponse? response = null;
        if (view is not null)
        {
            var rivalPronoun = view.Rival.Address == "she" ? "её" : "его";
            response = new ChallengeResponse(
                view.Challenge.Id.Value,
                "я",
                new FriendResponse(
                    view.Rival.Id.Value,
                    view.RivalWorld.Player.Name,
                    view.RivalWorld.Player.Avatar,
                    view.RivalWorld.BalanceKey,
                    view.RivalWorld.Player.Subscribers,
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
            new StartChallenge(PlayerId.From(request.FriendId)),
            cancellationToken);
        return TypedResults.Ok();
    }
}