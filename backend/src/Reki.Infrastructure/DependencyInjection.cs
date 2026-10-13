using System.Text.Json.Serialization.Metadata;
using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.DI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Application.Common.Safety;
using Reki.Infrastructure.AI;
using Reki.Infrastructure.Health;
using Reki.Infrastructure.Jobs;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Rendering;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.Instrumentation.OpenTelemetry;
using TickerQ.MongoDB.DependencyInjection;

namespace Reki.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IJsonTypeInfoResolver jsonContext)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IStoryCardRenderer, PngStoryCardRenderer>();
        services.AddObservability();
        services.AddBaseHealthChecks();
        services.AddMongoInfrastructure();
        services.AddLlmInfrastructure();
        services.AddGameJobs(configuration, jsonContext);

        return services;
    }

    private static void AddGameJobs(this IServiceCollection services, IConfiguration configuration, IJsonTypeInfoResolver jsonContext)
    {
        services.AddOptions<JobsOptions>().BindConfiguration(JobsOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.DashboardUsername) && !string.IsNullOrWhiteSpace(options.DashboardPassword), "Jobs dashboard requires a username and password.")
            .ValidateOnStart();
        var jobs = configuration.GetSection(JobsOptions.SectionName).Get<JobsOptions>() ?? new JobsOptions();
        var mongo = configuration.GetRequiredSection(MongoOptions.SectionName).Get<MongoOptions>() ?? throw new InvalidOperationException("Mongo configuration is required.");
        services.MapTicker<ResetGameDayJob>().WithCron("0 0 9 * * *").WithMaxConcurrency(1);
        services.MapTicker<RefreshRegisteredPlayersJob>(ServiceLifetime.Singleton).WithCron("0 * * * * *").WithMaxConcurrency(1);
        services.AddTickerQ(options =>
        {
            options.ConfigureScheduler(scheduler => scheduler.SchedulerTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));
            options.AddOperationalStore(store => store.UseExistingMongoClient(mongo.DatabaseName));
            options.WithJsonContext(jsonContext);
            options.AddOpenTelemetryInstrumentation();
            options.SetExceptionHandler<JobsExceptionHandler>();
            if (!jobs.ExecutionEnabled)
            {
                options.DisableBackgroundServices();
            }

            options.AddDashboard(dashboard =>
            {
                dashboard.SetBasePath("/admin/tickerq");
                dashboard.WithBasicAuth(jobs.DashboardUsername, jobs.DashboardPassword);
            });
        });
        services.AddSignalR().AddJsonProtocol(options =>
        {
            // TickerQ sends anonymous hub payloads outside its HTTP JSON context.
            options.PayloadSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        });
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
            .AddSingleton<LlmSpendPolicy>()
            .AddSingleton<OpenAiRoutingChatClient>()
            .AddChatClient(sp => sp.GetRequiredService<OpenAiRoutingChatClient>())
            .UseOpenTelemetry(
                sourceName: "Reki.GenAI",
                configure: telemetry => telemetry.EnableSensitiveData = false)
            .Use(innerClient => new DeepSeekThinkingChatClient(innerClient))
            .Use((innerClient, serviceProvider) => new PlayerSafetyChatClient(
                innerClient,
                serviceProvider.GetRequiredService<PlayerSafetyGuard>()))
            .Use(client => new LlmSpendChatClient(client));
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

        services.AddSingleton<MongoCompositionRoot>();
        services.AddSingleton<IGameStore, MongoGameStore>();
        services.AddMetrics();
        services.AddSingleton<IMongoClient>(sp => sp.GetRequiredService<MongoCompositionRoot>().MongoClient);
        services.AddSingleton<IMongoDatabase>(sp => sp.GetRequiredService<MongoCompositionRoot>().MongoDatabase);
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", null, ["ready"]);

        services
            .AddMongoMigrations(
                new MigrationOptions("mongodb"),
                locationConfig => locationConfig.LoadMigrationsFromAssembly(typeof(DependencyInjection).Assembly))
            .Replace(ServiceDescriptor.Singleton<IMigrator, RekiMigrator>());
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
                .AddSource(MongoTelemetry.ActivitySourceName, "Reki.GenAI", "TickerQ")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddMeter("Reki.GenAI", "Reki.Product")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithLogging(
                _ => { },
                logging => logging.IncludeScopes = true)
            .UseOtlpExporter();
    }
}
