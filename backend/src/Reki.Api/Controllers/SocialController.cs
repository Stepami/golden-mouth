using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Api.Requests.Social;
using Reki.Application.Social.Responses;

namespace Reki.Api.Controllers;

[ApiController]
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
        string starId,
        [FromQuery] string worldId,
        CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok(new DmResponse(new StarResponse(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0), string.Empty, [], [], [], 0)));

    [HttpPost("dm/{starId}", Name = "SendDirectMessage")]
    [ActionName("SendDirectMessage")]
    public Task<Ok<SendDirectMessageResponse>> SendDirectMessageAsync(
        string starId,
        DirectMessageRequest request,
        [FromQuery] string worldId,
        CancellationToken cancellationToken) =>
        Task.FromResult(TypedResults.Ok(new SendDirectMessageResponse(null, 0)));

    [HttpDelete("stars/{starId}/memory", Name = "ForgetStarMemory")]
    [ActionName("ForgetStarMemory")]
    public Task<Ok> ForgetStarMemoryAsync(
        string starId,
        ForgetMemoryRequest request,
        [FromQuery] string worldId,
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
