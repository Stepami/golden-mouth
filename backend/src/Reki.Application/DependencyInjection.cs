using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.AI.Serializers;
using Reki.Application.Posts.PublishPostCommand;
using Reki.Application.Telemetry;
using Reki.Domain.Posts;
using Reki.Domain.Worlds;

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
        services.AddSingleton<StarTurnPolicy>();
        services.AddSingleton<PostProgressionCalculator>();
        services.AddSingleton<ProductTelemetry>();
        services.AddSingleton<IStarAgentInstructions, StarAgentInstructions>();
        services.AddSingleton<IStarTurnGenerator, StarTurnGenerator>();
        services.AddSingleton<IStarContentGenerator, StarContentGenerator>();
        services.AddSingleton<IEncodeSerializer<GamePost>, GamePostEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<WorldStar>, WorldStarEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<GameComment>, GameCommentEncodeSerializer>();

        return services;
    }
}
