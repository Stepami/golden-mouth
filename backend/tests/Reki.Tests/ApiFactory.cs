using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MongoDb;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;
using WebMotions.Fake.Authentication.JwtBearer;

namespace Reki.Tests;

public sealed class ApiFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0.32-noble").Build();

    public string DatabaseName { get; } = $"reki_tests_{Guid.NewGuid():N}";

    public string MongoConnectionString => _mongo.GetConnectionString();

    public TestChatClient ChatClient { get; } = new();

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
        await _mongo.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Mongo:ConnectionString", MongoConnectionString);
        builder.UseSetting("Mongo:DatabaseName", DatabaseName);
        builder.UseSetting("Llm:BaseUrl", "https://llm.invalid/v1");
        builder.UseSetting("Llm:ApiKey", "placeholder");
        builder.UseSetting("Llm:Model", "gigachat3.5-432b-a28b");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<TimeProvider>();
            services.RemoveAll<IChatClient>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(
                new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero)));
            services.AddSingleton<IChatClient>(ChatClient);
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = FakeJwtBearerDefaults.AuthenticationScheme;
                })
                .AddFakeJwtBearer();
            services.AddAuthorization(options => options.AddPolicy(
                "TMiniApp",
                new AuthorizationPolicyBuilder(FakeJwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build()));
        });
    }
}