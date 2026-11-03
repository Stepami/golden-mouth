using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reki.Api.Auth;
using Reki.Api.Requests.Social;
using Reki.Application.Social.Responses;
using Reki.Application.Social.Responses.Challenge;
using Reki.Application.Social.Responses.Dm;
using Reki.Domain.Worlds;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.AppJwtPolicy)]
[Produces("application/json")]
[Route("/")]
public sealed class SocialController : ControllerBase
{
    [HttpGet("friends", Name = "GetFriends")]
    [ActionName("GetFriends")]
    public Task<Ok<IReadOnlyList<FriendResponse>>> GetFriendsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok<IReadOnlyList<FriendResponse>>([]));

    [HttpGet("dm/{starId}", Name = "GetDirectMessages")]
    [ActionName("GetDirectMessages")]
    public Task<Ok<DmResponse>> GetDirectMessagesAsync(
        StarId starId,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok(new DmResponse(new StarResponse(starId, string.Empty, string.Empty, string.Empty, string.Empty, 0), string.Empty, [], [], [], 0)));

    [HttpPost("dm/{starId}", Name = "SendDirectMessage")]
    [ActionName("SendDirectMessage")]
    public Task<Ok<SendDirectMessageResponse>> SendDirectMessageAsync(
        StarId starId,
        DirectMessageRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok(new SendDirectMessageResponse(null, 0)));

    [HttpDelete("stars/{starId}/memory", Name = "ForgetStarMemory")]
    [ActionName("ForgetStarMemory")]
    public Task<Ok> ForgetStarMemoryAsync(
        StarId starId,
        ForgetMemoryRequest request,
        [FromQuery, BindRequired] WorldId worldId,
        CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok());

    [HttpGet("challenges/active", Name = "GetActiveChallenge")]
    [ActionName("GetActiveChallenge")]
    public Task<Ok<ChallengeResponse?>> GetActiveChallengeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok<ChallengeResponse?>(null));

    [HttpPost("challenges", Name = "StartChallenge")]
    [ActionName("StartChallenge")]
    public Task<Ok> StartChallengeAsync(StartChallengeRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok());
}
