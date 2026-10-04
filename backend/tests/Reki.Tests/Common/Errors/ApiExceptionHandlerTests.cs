using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Reki.Tests.Serialization;
using TUnit.AspNetCore;

namespace Reki.Tests.Common.Errors;

[NotInParallel]
public sealed class ApiExceptionHandlerTests : WebApplicationTest<ApiFactory, Program>
{
    [Test]
    public async Task ExceptionHandler_WhenCustomExceptionIsThrown_ReturnsSafeProblemDetailsAsync()
    {
        // Arrange
        using HttpClient httpClient = Factory.CreateClient();

        // Act
        using HttpResponseMessage response = await httpClient.GetAsync("/_tests/exceptions/not-found");
        string json = await response.Content.ReadAsStringAsync();
        ProblemDetails problemDetails = JsonSerializer.Deserialize(
                json,
                GeneratedClientJsonContext.Default.ProblemDetails)
            ?? throw new InvalidOperationException("ProblemDetails response was empty.");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(problemDetails.Title).IsEqualTo("Resource not found");
        await Assert.That(problemDetails.Detail)
            .IsEqualTo("The requested resource was not found.");
        await Assert.That(json).DoesNotContain("Internal resource details");
    }
}
