using System.Globalization;
using Kot.MongoDB.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NSubstitute;
using Reki.Api;
using Reki.Application.Posts.ML;
using Reki.Domain.Player;
using Reki.Infrastructure.Auth;
using Reki.Infrastructure.ML.Nli;
using Reki.Tests.Persistence;
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
        var client = CreateClient();
        PlayerId? playerId = null;
        if (telegramUserId is { } id)
        {
            var externalId = ExternalUserId.Telegram(id);
            var players = Session.Database.GetCollection<PlayerProfile>("players");
            playerId = players.Find(item => item.ExternalUserIds.Contains(externalId)).FirstOrDefault()?.Id ?? PlayerId.New();
        }

        return new TestApiClient(client, playerId, telegramUserId, this);
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
        builder.UseSetting("Embedding:BaseUrl", Llm.EmbeddingBaseUrl);
        builder.UseSetting("Embedding:Model", "embedding");
        builder.UseSetting("Auth:Issuer", UseProductionEnvironment ? "https://localhost/" : "http://localhost/");
        builder.UseSetting("Auth:Audience", "reki-test");
        builder.UseSetting("Auth:ClientId", "reki-test-client");
        builder.UseSetting("Auth:SigningKey", Session.SigningKey);
        builder.UseSetting("Auth:EncryptionKey", Session.EncryptionKey);
        builder.UseSetting("Telegram:OAuthUrl", TelegramOAuthUrl ?? "https://example.invalid/");
        builder.UseSetting("Telegram:ApiUrl", "https://example.invalid/");
        builder.UseSetting("Telegram:BotToken", Session.TelegramBotToken);
        builder.UseSetting("Vk:ApplicationId", "123456");
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
            services.Replace(ServiceDescriptor.Singleton<IMigrationInstantiator, AtlasLocalMigrationInstantiator>());
            services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Session.Clock));
            services.RemoveAll<IValidateOptions<NliOptions>>();
            var nli = Substitute.For<INliScorer>();
            nli.ScoreAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
                .Returns(call => ValueTask.FromResult<IReadOnlyList<NliScores>>(
                    call.ArgAt<IReadOnlyList<string>>(1).Select(_ => new NliScores(0.5, 0.5)).ToArray()));
            services.Replace(ServiceDescriptor.Singleton(nli));
            var classifier = Substitute.For<IClassifier>();
            classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyDictionary<string, double>>(new Dictionary<string, double>
                {
                    ["insult"] = 0,
                    ["anger"] = 0,
                    ["sadness"] = 0,
                    ["fear"] = 0,
                    ["surprise"] = 0,
                    ["joy"] = 0,
                    ["no_emotion"] = 1,
                }));
            services.Replace(ServiceDescriptor.Singleton(classifier));
            if (!UseProductionAuthentication)
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddFakeJwtBearer(options =>
                    {
                        options.BearerValueType = FakeJwtBearerBearerValueType.Jwt;
                        ((FakeJwtBearerClaimsHandler)options.SecurityTokenClaimHandler).Options.MapInboundClaims = false;
                    });
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
