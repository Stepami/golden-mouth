using System.Text.Json;
using Reki.Api.Serialization;
using Reki.Tests.Generated;

namespace Reki.Tests.Common.Errors;

[NotInParallel]
[ClassDataSource<ApiFactory>(Shared = SharedType.PerTestSession)]
public sealed class ApiExceptionHandlerTests(ApiFactory factory)
{
    [Test]
    public async Task ExceptionHandler_WhenCustomExceptionIsThrown_ReturnsSafeProblemDetailsAsync()
    {
        using var api = factory.CreateApiClient(2001);
        PlayerApiException? exception = null;
        try
        {
            await api.Player.GetCurrentPlayerAsync(CancellationToken.None);
        }
        catch (PlayerApiException caught)
        {
            exception = caught;
        }

        if (exception is null)
        {
            throw new InvalidOperationException("The typed client did not surface the not-found response.");
        }

        var json = exception.Response;
        var problemDetails = JsonSerializer.Deserialize(
                json,
                ApiJsonSerializerContext.Default.ProblemDetails)
            ?? throw new InvalidOperationException("ProblemDetails response was empty.");

        await Assert.That(exception.StatusCode).IsEqualTo(404);
        await Assert.That(problemDetails.Title).IsEqualTo("Resource not found");
        await Assert.That(problemDetails.Detail)
            .IsEqualTo("The requested resource was not found.");
        await Assert.That(json).DoesNotContain("Player not found");
    }
}
