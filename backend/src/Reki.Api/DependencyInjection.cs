using Microsoft.AspNetCore.Mvc.Authorization;
using Reki.Api.Mapping;
using Reki.Api.OpenApi;
using Reki.Api.Serialization;
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
        });
        services
            .AddControllers(options => options.Filters.Add(
                new AuthorizeFilter(TgMiniAppAuthConstants.AuthenticationScheme)));
        services.AddSingleton<IApiMapper, ApiMapper>();
        services.AddSerialization();

        return services;
    }
}
