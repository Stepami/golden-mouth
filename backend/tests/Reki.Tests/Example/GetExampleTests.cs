using System.Net;
using Reki.Contracts.Example;
using Reki.Tests.Generated;
using TUnit.AspNetCore;

namespace Reki.Tests.Example;

[NotInParallel]
public sealed class GetExampleTests : WebApplicationTest<ApiFactory, Program>
{
    [Test]
    public async Task GetExample_WhenCalled_ReturnsServiceStatusAsync()
    {
        using HttpClient httpClient = Factory.CreateClient();
        var client = new ExampleClient(httpClient);

        ExampleResponse response = await client.GetExampleAsync();

        await Assert.That(response.Message).IsEqualTo("Reki backend is running.");
        await Assert.That(response.GeneratedAtUtc).IsNotEqualTo(default(DateTimeOffset));
    }

    [Test]
    public async Task SwaggerUi_WhenRequested_ReturnsHtmlAsync()
    {
        using HttpClient httpClient = Factory.CreateClient();
        using HttpResponseMessage response = await httpClient.GetAsync("/swagger/");
        string html = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(html).Contains("swagger-ui");
    }
}
