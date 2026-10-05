using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.Authorization;
using Reki.Api.Common.Errors;
using Reki.Api.Mapping;
using Reki.Api.Serialization;
using Reki.Application;
using Reki.Application.Common;
using Reki.Contracts.Events;
using Reki.Contracts.Player;
using Reki.Contracts.Posts;
using Reki.Contracts.Social;
using Reki.Contracts.Telemetry;
using TgMiniAppAuth;

namespace Reki.Api.Bootstrapping;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(ApplicationAssemblyMarker)];
            options.ServiceLifetime = ServiceLifetime.Singleton;
        });
        services.AddSingleton<GameDay>();
        services.AddSingleton<PlayerReader>();

        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi(options => options.AddOperationTransformer((operation, context, _) =>
        {
            var routeValues = context.Description.ActionDescriptor.RouteValues;

            if (routeValues.TryGetValue("controller", out var controller) &&
                routeValues.TryGetValue("action", out var action))
            {
                operation.OperationId = $"{controller}_{action}";
            }

            return Task.CompletedTask;
        }));
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
        options.Converters.Add(new JsonStringEnumConverter<SkillId>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<WorldId>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<Address>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<Drama>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<BirthGroup>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<FeedItemKind>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<DirectMessageAuthor>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<GameEventKind>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<LiveEventType>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<ClientLogLevel>(JsonNamingPolicy.CamelCase));
    }
}
