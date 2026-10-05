using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using NSubstitute;
using Testcontainers.MongoDb;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace Reki.Tests.Api;

public sealed class DevelopmentTelegramAuthenticationFactory
    : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0.32-noble").Build();

    public IMongoDatabase Database => new MongoClient(_mongo.GetConnectionString())
        .GetDatabase(DatabaseName);

    private string DatabaseName { get; } = $"reki_development_auth_tests_{Guid.NewGuid():N}";

    public HttpClient CreateProductionClient()
    {
        return WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();
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
        builder.UseSetting("Mongo:ConnectionString", _mongo.GetConnectionString());
        builder.UseSetting("Mongo:DatabaseName", DatabaseName);
        builder.UseSetting("Llm:BaseUrl", "https://llm.invalid/v1");
        builder.UseSetting("Llm:ApiKey", "placeholder");
        builder.UseSetting("Llm:Model", "test-model");
        builder.UseSetting("TelegramMiniAppAuthorizationOptions:Token", "test-token");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IChatClient>();
            services.AddSingleton(Substitute.For<IChatClient>());
        });
    }
}
