using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reki.Application.Posts;
using Reki.Application.Posts.AI.Comment;
using Reki.Application.Posts.AI.Feed;
using Reki.Application.Posts.AI.Post;
using Reki.Domain.Notifications;
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
        var mongo = configuration.GetRequiredSection(MongoOptions.SectionName).Get<MongoOptions>() ??
                    throw new InvalidOperationException("Mongo configuration is required.");

        services.MapTicker<GenerateStarFeedOnFeedOpenedJob, StarFeedRequest>().WithMaxConcurrency(2);
        services.MapTicker<GenerateStarFeedOnPostPublishedJob, StarFeedOnPostPublishedRequest>().WithMaxConcurrency(2);
        services.MapTicker<GenerateStarFeedOnCommentPublishedJob, StarFeedOnCommentPublishedRequest>()
            .WithMaxConcurrency(2);
        services.MapTicker<RestoreEnergyJob>(ServiceLifetime.Singleton).WithMaxConcurrency(1);
        services.MapTicker<RefreshRegisteredPlayersJob>(ServiceLifetime.Singleton).WithMaxConcurrency(1);
        services.MapTicker<RefreshDailyChallengesJob>(ServiceLifetime.Singleton).WithMaxConcurrency(1);
        services.MapTicker<PruneAuthJob>().WithMaxConcurrency(1);

        // Keep the existing cron function as the entry point of the chain.
        const string notificationSchedule = nameof(SendNotificationsJob);
        services.MapTicker(notificationSchedule, (_, _) => Task.CompletedTask).WithMaxConcurrency(1);
        services.MapTickerGroup("Notifications")
            .MapTicker<SendNotificationsJob, NotificationChannelType>("Deliver")
            .WithMaxConcurrency(2);

        services.AddTickerQ<RekiTimeTicker, CronTickerEntity>(options =>
        {
            options.UseApplicationNamespace("reki");
            options.SeedCronTickers(crons => crons
                .Add(nameof(RestoreEnergyJob), "11 */2 * * * *")
                .Add(nameof(RefreshRegisteredPlayersJob), "15 */30 * * * *")
                .Add(nameof(RefreshDailyChallengesJob), "0 0 9 * * *")
                .Add(nameof(PruneAuthJob), "0 0 3 * * *")
                .Add(notificationSchedule, cron => cron
                    .WithExpression("40 */5 * * * *")
                    .SkipIfAlreadyRunning(true)
                    .StartWith(notificationSchedule)
                    .ThenMany(branches =>
                    {
                        branches.Add<SendNotificationsJob, NotificationChannelType>(NotificationChannelType.Telegram);
                        branches.Add<SendNotificationsJob, NotificationChannelType>(NotificationChannelType.WebPush);
                    })));
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

        // The replacement must start before TickerQ initializes the scheduler.
        var licenseService = services.Single(service =>
            service.ServiceType == typeof(IHostedService) &&
            service.ImplementationType?.FullName == "TickerQ.BackgroundServices.TickerQLicenseHostedService");
        services[services.IndexOf(licenseService)] = ServiceDescriptor.Singleton<IHostedService, TickerQLicenseStateHostedService>();
        return services;
    }
}
