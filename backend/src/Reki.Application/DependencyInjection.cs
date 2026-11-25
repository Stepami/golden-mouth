using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reki.Application.Balance;
using Reki.Application.Notifications;
using Reki.Application.Notifications.Content;
using Reki.Application.Player.Challenges;
using Reki.Application.Posts.AddReplyCommand;
using Reki.Application.Posts.AI;
using Reki.Application.Posts.AI.Safety;
using Reki.Application.Posts.AI.Serializers;
using Reki.Application.Posts.CreatePostCommand;
using Reki.Application.Posts.CreatePostCommand.Progression;
using Reki.Application.Posts.CreatePostCommand.Progression.Score;
using Reki.Application.Posts.CreatePostCommand.Progression.Score.FlopTests;
using Reki.Application.Posts.LikePostCommand;
using Reki.Application.Posts.ML;
using Reki.Application.Posts.Responses;
using Reki.Application.Posts.Responses.Reply;
using Reki.Application.Telemetry;
using Reki.Application.Worlds;
using Reki.Domain.Player;
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
            options.PipelineBehaviors = [typeof(DailyProgressBehavior<,>)];
        });

        services.AddSingleton<IStateComparer<CreatePost, CreatePostResponse>, CreatePostStateComparer>();
        services.AddSingleton<IStateComparer<AddReply, ReplyResponse>, AddReplyStateComparer>();
        services.AddSingleton<IStateComparer<LikePost, Unit>, LikePostStateComparer>();

        services.AddSingleton<ViralityCalculator>();
        services.AddSingleton<PostNumbersCalculator>();
        services.AddSingleton<SubscriberDeltaCalculator>();
        services.AddSingleton<PostProgressionCalculator>();
        services.AddSingleton<PostTextAnalysis>();
        services.AddSingleton<PostScorer>();
        services.AddSingleton<IFlopTest, AngryAbuseFlopTest>();
        services.AddSingleton<IFlopTest, NearCopyFlopTest>();
        services.AddSingleton<IFlopTest, SadToneFlopTest>();
        services.AddSingleton<IFlopTest, AnxiousToneFlopTest>();
        services.AddSingleton<PostCrystalsCalculator>();

        services.AddSingleton<ProductTelemetry>();

        services.AddSingleton<PlayerSafetyGuard>();
        services.AddSingleton<StarTurnPolicy>();
        services.AddSingleton<IStarAgentInstructions, StarAgentInstructions>();
        services.AddSingleton<IStarTurnGenerator, StarTurnGenerator>();
        services.AddSingleton<IStarContentGenerator, StarContentGenerator>();

        services.AddSingleton<IEncodeSerializer<GamePost>, GamePostEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<WorldStar>, WorldStarEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<GameComment>, GameCommentEncodeSerializer>();
        services.AddSingleton<IEncodeSerializer<World>, WorldEncodeSerializer>();
        services.AddSingleton<IStarVectorStore, StarVectorStore>();

        services.AddSingleton<IWorldPlayerCache, WorldPlayerCache>();

        services.AddSingleton<INotificationContentFactory<PlayerProfile>, RegularContentFactory>();
        services.AddSingleton<INotificationContentFactory<DateOnly>, ChallengeContentFactory>();

        return services;
    }
}
