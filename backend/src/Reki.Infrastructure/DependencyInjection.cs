using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Reki.Application.Balance;
using Reki.Application.Posts.GetPublicStoryCard;
using Reki.Infrastructure.AI;
using Reki.Infrastructure.Auth;
using Reki.Infrastructure.Jobs;
using Reki.Infrastructure.Mongo;
using Reki.Infrastructure.RekiContext;
using Reki.Infrastructure.Rendering;
using Reki.Infrastructure.Telegram;

namespace Reki.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IJsonTypeInfoResolver jsonContext)
    {
        services.AddSingleton<IOptionsChangeTokenSource<GameBalance>, ConfigurationChangeTokenSource<GameBalance>>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IStoryCardRenderer, PngStoryCardRenderer>();

        services
            .AddRekiContext()
            .AddChatClients()
            .AddJobs(configuration, jsonContext)
            .AddMongo()
            .AddAuth()
            .AddTelegram(configuration);

        services.AddObservability();
        services.AddBaseHealthChecks();

        return services;
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
        services.AddMetrics();
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddSource(MongoTelemetry.ActivitySourceName, "Reki.GenAI", "TickerQ")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddProcessor<TelegramBotTelemetry>())
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
