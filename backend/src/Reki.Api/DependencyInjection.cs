using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi;
using Reki.Api.OpenApi;
using Reki.Api.Serialization;
using Reki.Domain.Telemetry;
using TgMiniAppAuth;

namespace Reki.Api;

internal static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer<NullableChallengeSchemaTransformer>();
            options.AddOperationTransformer<OperationMetadataTransformer>();
            options.AddDocumentTransformer<TelegramMiniAppSecurityTransformer>();
            options.AddSchemaTransformer((schema, context, _) =>
            {
                if (context.JsonTypeInfo.Type == typeof(SessionId))
                {
                    schema.Type = JsonSchemaType.String;
                }

                return Task.CompletedTask;
            });
        });
        services
            .AddControllers(options => options.Filters.Add(
                new AuthorizeFilter(TgMiniAppAuthConstants.AuthenticationScheme)));
        services.AddSerialization();

        return services;
    }
}
