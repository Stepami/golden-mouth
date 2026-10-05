using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Reki.Application.Game.Common;
using Reki.Infrastructure.Game.Persistence;
using Reki.Infrastructure.Game.Rendering;
using Reki.Infrastructure.Health;
using Reki.Infrastructure.Observability;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan MongoServerSelectionTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool initializeDatabase = true)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddObservability(configuration);
        services.AddBaseHealthChecks();
        services.AddMongoInfrastructure(initializeDatabase);

        return services;
    }

    private static void AddMongoInfrastructure(
        this IServiceCollection services,
        bool initializeDatabase)
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
            MongoOptions options = serviceProvider
                .GetRequiredService<IOptions<MongoOptions>>()
                .Value;

            MongoClientSettings settings = MongoClientSettings.FromConnectionString(
                options.ConnectionString);
            settings.ServerSelectionTimeout = MongoServerSelectionTimeout;

            return new MongoClient(settings);
        });

        services.AddSingleton(serviceProvider =>
        {
            MongoOptions options = serviceProvider
                .GetRequiredService<IOptions<MongoOptions>>()
                .Value;
            IMongoClient client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(options.DatabaseName);
        });
        services.AddSingleton<IGameStore, MongoGameStore>();
        services.AddSingleton<IStoryCardRenderer, PngStoryCardRenderer>();
        if (initializeDatabase)
        {
            services.AddHostedService<MongoGameIndexes>();
        }

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

    private static void AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection optionsSection = configuration.GetRequiredSection(
            OpenTelemetryOptions.SectionName);

        services
            .AddOptions<OpenTelemetryOptions>()
            .Bind(optionsSection)
            .Validate(
                options => options.TryGetEndpoint(out _),
                "OpenTelemetry:Endpoint must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        var options = optionsSection.Get<OpenTelemetryOptions>()
            ?? throw new InvalidOperationException(
                $"The {OpenTelemetryOptions.SectionName} configuration section is invalid.");

        if (!options.Enabled || !options.TryGetEndpoint(out var endpoint))
        {
            return;
        }

        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging()
            .UseOtlpExporter(OtlpExportProtocol.Grpc, endpoint);
    }
}
