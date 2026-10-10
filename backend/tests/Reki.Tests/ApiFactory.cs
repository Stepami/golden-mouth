using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MongoDb;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class ApiFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0.32-noble")
        .WithReplicaSet()
        .Build();

    public string DatabaseName { get; } = $"reki_tests_{Guid.NewGuid():N}";

    public string MongoConnectionString => _mongo.GetConnectionString();

    public OpenAiStub Llm { get; } = new();

    public TestApiClient CreateApiClient(long? telegramUserId = null)
    {
        return new TestApiClient(CreateClient(), telegramUserId);
    }

    public Task InitializeAsync()
    {
        return _mongo.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Llm.Dispose();
        await _mongo.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Mongo:ConnectionString", MongoConnectionString);
        builder.UseSetting("Mongo:DatabaseName", DatabaseName);
        builder.UseSetting("Llm:BaseUrl", Llm.BaseUrl);
        builder.UseSetting("Llm:ApiKey", "placeholder");
        builder.UseSetting("Llm:Model", "gigachat3.5-432b-a28b");
        builder.UseSetting("Llm:Temperature", "0.25");
        builder.UseSetting("Llm:MaxOutputTokens", "321");
        builder.UseSetting("Llm:TimeoutSeconds", "1");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(
                new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero)));

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
        });
    }
}
