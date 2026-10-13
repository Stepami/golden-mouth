using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Reki.Application.Common;
using Reki.Application.Common.Safety;
using Reki.Application.Telemetry;
using Reki.Domain.Player;
using TUnit.AspNetCore;

namespace Reki.Tests;

public sealed class ApiFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
    public required TestSessionFixture Session { get; init; }

    public ILogger<ProductTelemetry> ProductLogger { get; } = Substitute.For<ILogger<ProductTelemetry>>();

    public IContentModerator ContentModerator { get; } = Substitute.For<IContentModerator>();

    public bool UseProductionAuthentication { get; init; }

    public double? FeedOpenProbability { get; init; }

    public OpenAiStub Llm => Session.Llm;

    public string DatabaseName => Session.DatabaseName;

    public string MongoConnectionString => Session.ConnectionString;

    public TestApiClient CreateApiClient(long? telegramUserId = null)
    {
        return new TestApiClient(CreateClient(), telegramUserId);
    }

    public void ResetContentModerator()
    {
        ContentModerator.ClearSubstitute();
        ContentModerator.ReviewAsync(Arg.Any<PlayerProfile>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new ContentReview(SafetyDecision.Allowed, "allowed"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jobs:ExecutionEnabled", "false");
        builder.UseSetting("Mongo:ConnectionString", MongoConnectionString);
        builder.UseSetting("Mongo:DatabaseName", DatabaseName);
        builder.UseSetting("Llm:BaseUrl", Llm.BaseUrl);
        builder.UseSetting("Llm:ApiKey", "placeholder");
        builder.UseSetting("Llm:Model", "gigachat3.5-432b-a28b");
        builder.UseSetting("Llm:Temperature", "0.25");
        builder.UseSetting("Llm:TimeoutSeconds", "1");
        builder.ConfigureServices(services =>
        {
            ProductLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
            services.AddSingleton(ProductLogger);
            ResetContentModerator();
            services.Replace(ServiceDescriptor.Singleton(ContentModerator));
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Session.Clock));
            if (FeedOpenProbability is { } probability)
            {
                services.AddSingleton<IOptions<GameBalance>>(provider => Options.Create(
                    provider.GetRequiredService<IOptionsFactory<GameBalance>>().Create(Options.DefaultName)
                        with { StarFeedOpenProbability = probability }));
            }

            if (!UseProductionAuthentication)
            {
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ => { });
                services.AddAuthorization(options => options.AddPolicy(
                    "TMiniApp",
                    new AuthorizationPolicyBuilder(TestAuthenticationHandler.SchemeName)
                        .RequireAuthenticatedUser()
                        .Build()));
            }
        });
    }
}
