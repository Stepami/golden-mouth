using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Invites;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.AI.Serializers;
using Reki.Application.Posts.CreatePostCommand.Progression;
using Reki.Application.Posts.CreatePostCommand.Progression.Score;
using Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;
using Reki.Application.Posts.ML;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
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
        services.AddSingleton<PlayerSafetyGuard>();
        services.AddSingleton<IWorldPlayerCache, WorldPlayerCache>();
        services.AddSingleton<IInvitationService, InvitationService>();
        services.AddSingleton<StarTurnPolicy>();
        services.AddSingleton<PostProgressionCalculator>();
        services.AddSingleton<PostTextAnalysis>();
        services.AddSingleton<PostScorer>();
        services.AddSingleton<IFlopTest, AngryAbuseFlopTest>();
        services.AddSingleton<IFlopTest, NearCopyFlopTest>();
        services.AddSingleton<IFlopTest, SadToneFlopTest>();
        services.AddSingleton<IFlopTest, AnxiousToneFlopTest>();
        services.AddSingleton<ProductTelemetry>();
        services.AddSingleton<IStarAgentInstructions, StarAgentInstructions>();
        services.AddSingleton<IStarTurnGenerator, StarTurnGenerator>();
        services.AddSingleton<IStarContentGenerator, StarContentGenerator>();
        services.AddSingleton<IEncodeSerializer<GamePost>, GamePostEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<WorldStar>, WorldStarEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<GameComment>, GameCommentEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<World>, WorldEncodeSerializer>();
        services.AddSingleton<IStarVectorStore, StarVectorStore>();

        return services;
    }
}
