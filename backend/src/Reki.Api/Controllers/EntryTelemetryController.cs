using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reki.Api.Requests.Telemetry.Landing;
using Reki.Infrastructure.Auth;

namespace Reki.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthConstants.Policies.RekiAppJwt)]
[Route("entry-events")]
[Produces("application/json")]
public sealed class EntryTelemetryController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("landing-entry")]
    [RequestSizeLimit(8192)]
    [HttpPost(Name = "SubmitLandingEntries")]
    [ActionName("SubmitLandingEntries")]
    public async Task<NoContent> SubmitAsync(LandingEntryBatchRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(request.ToCommand(), cancellationToken);
        return TypedResults.NoContent();
    }

    [EnableRateLimiting("landing-entry")]
    [RequestSizeLimit(2048)]
    [HttpPost("link", Name = "LinkLandingProfile")]
    [ActionName("LinkLandingProfile")]
    public async Task<NoContent> LinkAsync(LandingVisitRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(request.ToLinkCommand(), cancellationToken);
        return TypedResults.NoContent();
    }
}
