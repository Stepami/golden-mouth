using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Example.GetExample;
using Reki.Contracts.Example;

namespace Reki.Api.Example.GetExample;

[ApiController]
[Produces("application/json")]
[Route("api/v1/example")]
public sealed partial class ExampleController(
    ISender sender,
    ExampleMapper mapper,
    ILogger<ExampleController> logger)
    : ControllerBase
{
    [HttpGet(Name = "GetExample")]
    [ActionName("GetExample")]
    public async Task<Ok<ExampleResponse>> GetExampleAsync(
        CancellationToken cancellationToken)
    {
        var query = new GetExampleQuery();
        GetExampleResult result = await sender.Send(query, cancellationToken);
        ExampleResponse response = mapper.Map(result);

        LogRequestCompleted();
        return TypedResults.Ok(response);
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Example status request completed")]
    private partial void LogRequestCompleted();
}
