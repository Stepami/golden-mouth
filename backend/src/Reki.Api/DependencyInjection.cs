using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Reki.Api.OpenApi;
using Reki.Api.Serialization;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using TgMiniAppAuth;
using Vogen;

[assembly: VogenDefaults(openApiSchemaCustomizations: OpenApiSchemaCustomizations.GenerateOpenApiMappingExtensionMethod)]

namespace Reki.Api;

[OpenApiMarker<WorldId>]
[OpenApiMarker<StarId>]
[OpenApiMarker<SessionId>]
internal static partial class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = info => info.Kind == JsonTypeInfoKind.None && !info.Type.IsEnum
                ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(info);
            options.MapVogenTypesInDependencyInjection();
            options.AddSchemaTransformer<NullableChallengeSchemaTransformer>();
            options.AddOperationTransformer<OperationMetadataTransformer>();
            options.AddDocumentTransformer<TelegramMiniAppSecurityTransformer>();
        });
        services
            .AddControllers(options => options.Filters.Add(
                new AuthorizeFilter(TgMiniAppAuthConstants.AuthenticationScheme)));
        services.AddSerialization();

        return services;
    }
}
