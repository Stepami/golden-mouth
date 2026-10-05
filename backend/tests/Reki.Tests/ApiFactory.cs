using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace Reki.Tests;

public sealed class ApiFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly MongoDbContainer mongo = new MongoDbBuilder("mongo:8.0.32-noble").Build();

    public string DatabaseName { get; } = $"reki_tests_{Guid.NewGuid():N}";

    public string MongoConnectionString => mongo.GetConnectionString();

    public Task InitializeAsync()
    {
        return mongo.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await mongo.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Mongo:ConnectionString", MongoConnectionString);
        builder.UseSetting("Mongo:DatabaseName", DatabaseName);
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
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
            services
                .AddControllers()
                .AddApplicationPart(typeof(TestExceptionController).Assembly);
        });
    }
}
