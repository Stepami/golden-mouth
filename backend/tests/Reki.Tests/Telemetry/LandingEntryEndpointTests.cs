using System.Net;
using System.Text;
using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Reki.Api.Controllers;
using Reki.Api.Serialization;
using WebMotions.Fake.Authentication.JwtBearer;

namespace Reki.Tests.Telemetry;

public sealed class LandingEntryEndpointTests
{
    private const string Visit = """
        {"visitorId":"11111111-1111-4111-8111-111111111111",
        "entryId":"22222222-2222-4222-8222-222222222222",
        "variant":"worlds","initialVariant":"worlds","source":"link","switched":false}
        """;

    [Test]
    [Arguments("worlds")]
    [Arguments("hogwarts")]
    [Arguments("infobiz")]
    [Arguments("evening")]
    [Arguments("houses")]
    public async Task AnonymousVisitorCanReportLandingBeforeRegistrationAsync(string variant)
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync("/entry-events", Body("landing_view", variant));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task AnonymousVisitorCannotLinkAProfileAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync(
            "/entry-events/link",
            new StringContent(Visit, Encoding.UTF8, "application/json"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PublicRouteRejectsForgedPublicationEventAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync("/entry-events", Body("post_scored"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PublicRouteRejectsNullBatchItemsAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync(
            "/entry-events",
            new StringContent("{\"events\":[null]}", Encoding.UTF8, "application/json"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    private static StringContent Body(string name, string variant = "worlds") => new(
        "{\"events\":[{\"eventId\":\"33333333-3333-4333-8333-333333333333\",\"name\":\"" +
        name + "\",\"timestamp\":1,\"visit\":" + Visit.Replace("worlds", variant, StringComparison.Ordinal) + "}]}",
        Encoding.UTF8,
        "application/json");

    private static async Task<WebApplication> ServerAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var services = builder.Services;
        services.AddSingleton(Substitute.For<ISender>());
        services.AddControllers().AddApplicationPart(typeof(EntryTelemetryController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
            });
        services.AddAuthentication(FakeJwtBearerDefaults.AuthenticationScheme).AddFakeJwtBearer();
        services.AddAuthorization(options => options.AddPolicy("RekiAppJwt", policy => policy
            .AddAuthenticationSchemes(FakeJwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()));
        services.AddRateLimiter(options => options.AddFixedWindowLimiter("landing-entry", limiter =>
        {
            limiter.PermitLimit = 600;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
        }));

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }
}
