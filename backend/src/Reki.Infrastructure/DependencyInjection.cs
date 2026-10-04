using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Reki.Infrastructure.Health;
using Reki.Infrastructure.Persistence;

namespace Reki.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan MongoServerSelectionTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        InfrastructureMode mode)
    {
        services.AddSingleton(TimeProvider.System);
        AddObservability(services, configuration);
        AddBaseHealthChecks(services);

        if (mode == InfrastructureMode.Production)
        {
            AddProductionInfrastructure(services);
        }

        return services;
    }

    private static void AddProductionInfrastructure(IServiceCollection services)
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

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", null, ["ready"]);
    }

    private static void AddBaseHealthChecks(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy(),
                ["live"]);
    }

    private static void AddObservability(
        IServiceCollection services,
        IConfiguration configuration)
    {
        OpenTelemetryBuilder telemetry = services
            .AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation());

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }
    }
}