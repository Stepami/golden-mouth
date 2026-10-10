using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.Authorization;
using Reki.Api.Common.Errors;
using Reki.Api.Mapping;
using Reki.Api.OpenApi;
using Reki.Api.Serialization;
using Reki.Application;
using Reki.Application.Common;
using Reki.Application.Common.Safety;
using Reki.Application.Posts;
using Reki.Application.Social;
using Reki.Application.Telemetry;
using TgMiniAppAuth;

namespace Reki.Api.Bootstrapping;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(ApplicationAssemblyMarker)];
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });
        services.AddSingleton<GameDay>();
        services.AddSingleton<PlayerReader>();
        services.AddSingleton<PlayerSafetyGuard>();
        services.AddSingleton<ChallengeLifecycle>();
        services.AddSingleton<PostProgressionCalculator>();
        services.AddSingleton<ProductTelemetry>();
        services.AddSingleton<StarTurnGenerator>();

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
        foreach (JsonConverter converter in ApiJsonSerializerContext.Default.Options.Converters)
        {
            options.Converters.Add(converter);
        }
    }
}
