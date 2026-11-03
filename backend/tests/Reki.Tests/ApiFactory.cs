using System.Globalization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reki.Api;
using Reki.Infrastructure.Auth;
using TUnit.AspNetCore;
using WebMotions.Fake.Authentication.JwtBearer;

namespace Reki.Tests;

public sealed class ApiFactory : TestWebApplicationFactory<Program>
{
    [ClassDataSource<TestSessionFixture>(Shared = SharedType.PerTestSession)]
    public required TestSessionFixture Session { get; init; }

    public bool UseProductionAuthentication { get; init; }

    public bool UseProductionEnvironment { get; init; }

    public string? TelegramOAuthUrl { get; init; }

    public double? FeedOpenProbability { get; init; }

    public OpenAiStub Llm => Session.Llm;

    public string DatabaseName => Session.DatabaseName;

    public string MongoConnectionString => Session.ConnectionString;

    public TestApiClient CreateApiClient(long? telegramUserId = null)
    {
        return new TestApiClient(CreateClient(), telegramUserId);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(UseProductionEnvironment ? "Production" : "Development");
        if (UseProductionEnvironment)
        {
            builder.UseSetting("ForwardedHeaders_Enabled", "true");
        }

        builder.UseSetting("Jobs:ExecutionEnabled", "false");
        builder.UseSetting("Mongo:ConnectionString", MongoConnectionString);
        builder.UseSetting("Mongo:DatabaseName", DatabaseName);
        builder.UseSetting("Llm:BaseUrl", Llm.BaseUrl);
        builder.UseSetting("Llm:ApiKey", "placeholder");
        builder.UseSetting("Llm:Model", "gigachat3.5-432b-a28b");
        builder.UseSetting("Llm:Temperature", "0.25");
        builder.UseSetting("Llm:TimeoutSeconds", "1");
        builder.UseSetting("Auth:Issuer", UseProductionEnvironment ? "https://localhost/" : "http://localhost/");
        builder.UseSetting("Auth:Audience", "reki-test");
        builder.UseSetting("Auth:ClientId", "reki-test-client");
        builder.UseSetting("Auth:SigningKey", Session.SigningKey);
        builder.UseSetting("Auth:EncryptionKey", Session.EncryptionKey);
        builder.UseSetting("Telegram:OAuthUrl", TelegramOAuthUrl ?? "https://example.invalid/");
        builder.UseSetting("Telegram:ApiUrl", "https://example.invalid/");
        builder.UseSetting("Telegram:BotToken", Session.TelegramBotToken);
        builder.UseSetting("Auth:AccessTokenLifetime", "00:15:00");
        builder.UseSetting("Auth:RefreshTokenLifetime", "30.00:00:00");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(FeedOpenProbability is { } probability
                ? new Dictionary<string, string?> { ["feed:open_probability"] = probability.ToString(CultureInfo.InvariantCulture) }
                : []);
        });
        builder.ConfigureServices(services =>
        {
            services.AddFakeLogging();
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Session.Clock));
            if (!UseProductionAuthentication)
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddFakeJwtBearer(options => options.BearerValueType = FakeJwtBearerBearerValueType.Jwt);
                services.PostConfigure<AuthorizationOptions>(options =>
                    options.AddPolicy(AuthConstants.Policies.RekiAppJwt, new AuthorizationPolicyBuilder(FakeJwtBearerDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .Build()));
            }
        });
        if (TelegramOAuthUrl is not null)
        {
            builder.ConfigureTestServices(services =>
                services.Configure<JwtBearerOptions>(AuthConstants.Schemes.TelegramLogin, options => options.RequireHttpsMetadata = false));
        }
    }
}
