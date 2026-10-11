using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Safety;
using Reki.Infrastructure.AI;
using Reki.Infrastructure.Health;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Rendering;

namespace Reki.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan MongoServerSelectionTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IStoryCardRenderer, PngStoryCardRenderer>();
        services.AddObservability();
        services.AddBaseHealthChecks();
        services.AddMongoInfrastructure();
        services.AddLlmInfrastructure();

        return services;
    }

    public static IServiceCollection AddMongoIndexInitialization(this IServiceCollection services)
    {
        services.AddHostedService<MongoGameIndexes>();
        return services;
    }

    private static void AddLlmInfrastructure(this IServiceCollection services)
    {
        services
            .AddOptions<LlmOptions>()
            .BindConfiguration(LlmOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           uri.Scheme is "http" or "https",
                "Llm:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        services
            .AddOptions<LlmTurnSettings>()
            .BindConfiguration(LlmOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddSingleton<OpenAiRoutingChatClient>()
            .AddChatClient(sp => sp.GetRequiredService<OpenAiRoutingChatClient>())
            .UseOpenTelemetry(
                sourceName: "Reki.GenAI",
                configure: telemetry => telemetry.EnableSensitiveData = false)
            .Use(innerClient => new DeepSeekThinkingChatClient(innerClient))
            .Use((innerClient, serviceProvider) => new PlayerSafetyChatClient(
                innerClient,
                serviceProvider.GetRequiredService<PlayerSafetyGuard>()));
    }

    private static void AddMongoInfrastructure(this IServiceCollection services)
    {
        services
            .AddOptions<MongoOptions>()
            .BindConfiguration(MongoOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out _),
                "Mongo:ConnectionString must be an absolute MongoDB URI.")
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MongoOptions>>()
                .Value;

            var settings = MongoClientSettings.FromConnectionString(
                options.ConnectionString);
            settings.ServerSelectionTimeout = MongoServerSelectionTimeout;

            return new MongoClient(settings);
        });

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MongoOptions>>()
                .Value;
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });
        services.AddSingleton<IGameStore, MongoGameStore>();
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", null, ["ready"]);
    }

    private static void AddBaseHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                ["live"]);
    }

    private static void AddObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(MongoTelemetry.ActivitySourceName, "Reki.GenAI")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter("Reki.GenAI")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging(
                _ => { },
                logging => logging.IncludeScopes = true)
            .UseOtlpExporter();
    }
}