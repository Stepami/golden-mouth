using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Reki.Api.Common.Errors;
using Reki.Api.Example.GetExample;
using Reki.Api.Serialization;
using Reki.Application;

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
            .AddControllers()
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
        services.AddSingleton<ExampleMapper>();
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        IJsonTypeInfoResolver resolver = ApiJsonSerializerContext.Default;
        options.TypeInfoResolverChain.Insert(0, resolver);
    }
}
