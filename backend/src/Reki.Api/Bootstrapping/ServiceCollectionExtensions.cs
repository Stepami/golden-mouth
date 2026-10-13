using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;
using Reki.Api.Common;
using Reki.Api.Common.Errors;
using Reki.Api.Mapping;
using Reki.Api.OpenApi;
using Reki.Application;
using Reki.Application.Common;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using Reki.Infrastructure.Serialization;
using TgMiniAppAuth;

namespace Reki.Api.Bootstrapping;

internal static class ServiceCollectionExtensions
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
        services.AddSingleton<ChallengeLifecycle>();
        services.AddSingleton<PostProgressionCalculator>();
        services.AddSingleton<ProductTelemetry>();
        services.AddSingleton<StarTurnGenerator>();

        services.AddSingleton<IContentModerator, ContentModerator>();
        services.AddSingleton<IRekiContextAccessor, HttpRekiContextAccessor>();
        services.AddScoped<IRekiContext, RekiContext>();

        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.AddOperationTransformer<OperationMetadataTransformer>();
            options.AddDocumentTransformer<TelegramMiniAppSecurityTransformer>();
        });
        services
            .AddControllers(options => options.Filters.Add(
                new AuthorizeFilter(TgMiniAppAuthConstants.AuthenticationScheme)))
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
        services.AddSingleton<ApiMapper>();
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        IJsonTypeInfoResolver resolver = ApiJsonSerializerContext.Default;
        options.TypeInfoResolverChain.Insert(0, resolver);
        foreach (var converter in ApiJsonSerializerContext.Default.Options.Converters)
        {
            options.Converters.Add(converter);
        }
    }
}
