using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Common;
using Reki.Application.Common.Balance;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Application.Telemetry;

namespace Reki.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<GameBalance>()
            .Validate(
                balance => balance.StarFeedOpenProbability is >= 0 and <= 1,
                "Feed open probability must be between zero and one.")
            .ValidateOnStart();
        services.AddTransient<IOptionsFactory<GameBalance>, GameBalanceOptionsFactory>();
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(ApplicationAssemblyMarker)];
            options.ServiceLifetime = ServiceLifetime.Singleton;
        });
        services.AddSingleton<GameDayPeriod>();
        services.AddSingleton<PlayerSafetyGuard>();
        services.AddSingleton<PostProgressionCalculator>();
        services.AddSingleton<ProductTelemetry>();
        services.AddSingleton<StarTurnGenerator>();

        return services;
    }
}
