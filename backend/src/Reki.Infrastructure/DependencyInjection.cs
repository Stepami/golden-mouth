using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Reki.Application.Common;
using Reki.Application.Common.AI;
using Reki.Infrastructure.AI;
using Reki.Infrastructure.Health;
using Reki.Infrastructure.Persistence;
using Reki.Infrastructure.Rendering;

namespace Reki.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan MongoServerSelectionTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool initializeDatabase = true,
        bool initializeLlm = true)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddObservability();
        services.AddBaseHealthChecks();
        services.AddMongoInfrastructure(initializeDatabase);
        if (initializeLlm)
        {
            services.AddLlmInfrastructure();
        }

        return services;
    }

    private static void AddLlmInfrastructure(this IServiceCollection services)
    {
        services
            .AddOptions<LlmOptions>()
            .BindConfiguration(LlmOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) &&
                    uri.Scheme is "http" or "https",
                "Llm:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
        {
            LlmOptions options = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
            return new LlmTurnSettings(
                options.Model,
                0.4f,
                700,
                TimeSpan.FromSeconds(options.TimeoutSeconds));
        });
        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            LlmOptions options = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
            var clientOptions = new OpenAIClientOptions
            {
                ClientLoggingOptions = new ClientLoggingOptions { EnableLogging = false },
                Endpoint = new Uri(options.BaseUrl),
                NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds),
                RetryPolicy = new ClientRetryPolicy(),
            };
            var client = new OpenAIClient(new ApiKeyCredential(options.ApiKey), clientOptions);
            IChatClient inner = client.GetChatClient(options.Model).AsIChatClient();
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new ChatClientBuilder(inner)
                .UseOpenTelemetry(
                    loggerFactory,
                    "Reki.GenAI",
                    telemetry => telemetry.EnableSensitiveData = false)
                .Use(innerClient => new DeepSeekThinkingChatClient(innerClient))
                .Build(serviceProvider);
        });
    }

    private static void AddMongoInfrastructure(
        this IServiceCollection services,
        bool initializeDatabase)
    {
        MongoClassMappings.Register();
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
