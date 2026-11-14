using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Posts;
using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.AI.Post;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnCommentPublished;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnFeedOpened;
using Reki.Infrastructure.Jobs.GenerateStarFeed.OnPostPublished;
using Reki.Infrastructure.Mongo.Options;
using TickerQ.DependencyInjection;
using TickerQ.Instrumentation.OpenTelemetry;
using TickerQ.MongoDB.DependencyInjection;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;

namespace Reki.Infrastructure.Jobs;

internal static class DependencyInjection
{
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        IJsonTypeInfoResolver jsonContext)
    {
        services.AddOptions<JobsOptions>().BindConfiguration(JobsOptions.SectionName);

        services.AddSingleton<IStarFeedScheduler<ScheduleFromFeedRequest>, StarFeedOnFeedOpenedScheduler>();
        services.AddSingleton<IStarFeedScheduler<ScheduleFromPostRequest>, StarFeedOnPostPublishedScheduler>();
        services.AddSingleton<IStarFeedScheduler<ScheduleFromCommentRequest>, StarFeedOnCommentPublishedScheduler>();

        var jobs = configuration.GetSection(JobsOptions.SectionName).Get<JobsOptions>() ?? new JobsOptions();
        var mongo = configuration.GetRequiredSection(MongoOptions.SectionName).Get<MongoOptions>() ??
                    throw new InvalidOperationException("Mongo configuration is required.");

        services.AddScoped<GenerateStarFeedOnFeedOpenedJob>();
        services.AddScoped<GenerateStarFeedOnPostPublishedJob>();
        services.AddScoped<GenerateStarFeedOnCommentPublishedJob>();
        services.AddSingleton<RestoreEnergyJob>();
        services.AddSingleton<RefreshRegisteredPlayersJob>();
        services.AddScoped<PruneAuthJob>();
        services.AddSingleton<SendNotificationsJob>();

        // TickerQ's registry is process-wide, but each host needs its own job services.
        if (!TickerFunctionProvider.IsBuilt)
        {
            services.MapTicker<GenerateStarFeedOnFeedOpenedJob, StarFeedRequest>().WithMaxConcurrency(2);
            services.MapTicker<GenerateStarFeedOnPostPublishedJob, StarFeedOnPostPublishedRequest>().WithMaxConcurrency(2);
            services.MapTicker<GenerateStarFeedOnCommentPublishedJob, StarFeedOnCommentPublishedRequest>()
                .WithMaxConcurrency(2);
            services.MapTicker<RestoreEnergyJob>(ServiceLifetime.Singleton).WithMaxConcurrency(1);
            services.MapTicker<RefreshRegisteredPlayersJob>(ServiceLifetime.Singleton).WithMaxConcurrency(1);
            services.MapTicker<PruneAuthJob>().WithMaxConcurrency(1);
            services.MapTicker<SendNotificationsJob>(ServiceLifetime.Singleton).WithMaxConcurrency(1);
        }

        services.AddTickerQ<RekiTimeTicker, CronTickerEntity>(options =>
        {
            options.UseApplicationNamespace("reki");
            if (jobs.LicensePath is not null)
            {
                options.UseLicense(jobs.LicensePath);
            }

            options.SeedCronTickers(crons => crons
                .Add(nameof(RestoreEnergyJob), "0 * * * * *")
                .Add(nameof(RefreshRegisteredPlayersJob), "0 * * * * *")
                .Add(nameof(PruneAuthJob), "0 0 3 * * *")
                .Add(nameof(SendNotificationsJob), "0 */5 * * * *"));
            options.ConfigureScheduler(scheduler =>
                scheduler.SchedulerTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"));
            options.AddOperationalStore(store => store.UseExistingMongoClient(mongo.DatabaseName));
            options.WithJsonContext(jsonContext);
            options.AddOpenTelemetryInstrumentation();
            options.SetExceptionHandler<JobsExceptionHandler>();
            if (!jobs.ExecutionEnabled)
            {
                options.DisableBackgroundServices();
            }
        });
        return services;
    }
}
