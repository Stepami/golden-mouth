using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Common.Exceptions;

namespace Reki.Tests;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("_tests/exceptions")]
public sealed class TestExceptionController : ControllerBase
{
    [HttpGet("not-found")]
    public NotFound ThrowNotFound()
    {
        throw new ResourceNotFoundException("Internal resource details must not leak.");
    }
}
