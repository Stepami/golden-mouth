using System.Net;
using System.Text;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Reki.Api.Controllers;
using Reki.Api.Serialization;
using Reki.Application.RekiContext;
using WebMotions.Fake.Authentication.JwtBearer;

namespace Reki.Tests.Telemetry;

public sealed class LandingEntryEndpointTests
{
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
        using var response = await client.PostAsync("/events", Body("landing_view", variant));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task AnonymousVisitorCannotLinkAProfileAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync(
            "/events",
            Body("landing_profile_link"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PublicRouteRejectsForgedPublicationEventAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync("/events", Body("post_scored"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PublicRouteRejectsNullBatchItemsAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync(
            "/events",
            new StringContent("{\"events\":[null]}", Encoding.UTF8, "application/json"));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [Arguments("landing_first_post", "worlds", "campaign_a")]
    [Arguments("landing_view", "unknown", "campaign_a")]
    [Arguments("landing_view", "worlds", "person@example.test")]
    public async Task PublicRouteRejectsInvalidFieldsAsync(string name, string variant, string campaign)
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var response = await client.PostAsync("/events", Body(name, variant, campaign));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AnonymousBatchSizeIsLimitedAsync()
    {
        await using var server = await ServerAsync();
        using var client = server.GetTestClient();
        using var content = Body("landing_view");
        using var oversized = new StringContent(await content.ReadAsStringAsync() + new string(' ', 9000), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/events", oversized);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AnonymousRequestsAreRateLimitedAsync()
    {
        await using var server = await ServerAsync(1);
        using var client = server.GetTestClient();
        using var first = await client.PostAsync("/events", Body("landing_view"));
        using var second = await client.PostAsync("/events", Body("landing_view"));
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    private static StringContent Body(string name, string variant = "worlds", string campaign = "campaign_a") => new(
        $$$"""
        {"session":"11111111-1111-4111-8111-111111111111","platform":"web","experiments":{},
         "events":[{"screen":"landing","name":"{{{name}}}","t":{{{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}},"props":{
          "event_id":"33333333-3333-4333-8333-333333333333",
          "visitor_id":"11111111-1111-4111-8111-111111111111",
          "entry_id":"22222222-2222-4222-8222-222222222222",
          "landing_variant":"{{{variant}}}","landing_initial_variant":"{{{variant}}}",
          "landing_source":"link","landing_switched":false,"landing_utm_campaign":"{{{campaign}}}"}}]}
        """,
        Encoding.UTF8,
        "application/json");

    private static async Task<WebApplication> ServerAsync(int permitLimit = 600)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var services = builder.Services;
        services.AddSingleton(Substitute.For<ISender>());
        services.AddSingleton(Substitute.For<IRekiContext>());
        services.AddSerialization();
        services.AddControllers().AddApplicationPart(typeof(TelemetryController).Assembly);
        services.AddAuthentication(FakeJwtBearerDefaults.AuthenticationScheme).AddFakeJwtBearer();
        services.AddAuthorization(options => options.AddPolicy("RekiAppJwt", policy => policy
            .AddAuthenticationSchemes(FakeJwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddFixedWindowLimiter("landing-entry", limiter =>
        {
            limiter.PermitLimit = permitLimit;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            });
        });

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
