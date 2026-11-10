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
        var mongo = configuration.GetRequiredSection(MongoOptions.SectionName).Get<MongoOptions>() ?? throw new InvalidOperationException("Mongo configuration is required.");

        services.MapTicker<GenerateStarFeedOnFeedOpenedJob, StarFeedRequest>().WithMaxConcurrency(2);
        services.MapTicker<GenerateStarFeedOnPostPublishedJob, StarFeedOnPostPublishedRequest>().WithMaxConcurrency(2);
        services.MapTicker<GenerateStarFeedOnCommentPublishedJob, StarFeedOnCommentPublishedRequest>().WithMaxConcurrency(2);
        services.MapTicker<ResetGameDayJob>(ServiceLifetime.Singleton)
            .WithCron("0 0 9 * * *")
            .WithMaxConcurrency(1);
        services.MapTicker<RestoreEnergyJob>(ServiceLifetime.Singleton)
            .WithCron("0 * * * * *")
            .WithMaxConcurrency(1);
        services.MapTicker<RefreshRegisteredPlayersJob>(ServiceLifetime.Singleton)
            .WithCron("0 * * * * *")
            .WithMaxConcurrency(1);
        services.MapTicker<PruneAuthJob>()
            .WithCron("0 0 3 * * *")
            .WithMaxConcurrency(1);
        services.AddTickerQ<RekiTimeTicker, CronTickerEntity>(options =>
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
        });
        return services;
    }
}
