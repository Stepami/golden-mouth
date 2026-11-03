using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Reki.Api.OpenApi;
using Reki.Api.Serialization;
using Reki.Domain.Events;
using Reki.Domain.Player;
using Reki.Domain.Posts;
using Reki.Domain.Telemetry;
using Reki.Domain.Worlds;
using Vogen;

[assembly: VogenDefaults(openApiSchemaCustomizations: OpenApiSchemaCustomizations.GenerateOpenApiMappingExtensionMethod)]

namespace Reki.Api;

[OpenApiMarker<WorldId>]
[OpenApiMarker<StarId>]
[OpenApiMarker<SessionId>]
[OpenApiMarker<PlayerId>]
[OpenApiMarker<PostId>]
[OpenApiMarker<CommentId>]
[OpenApiMarker<AuthorId>]
[OpenApiMarker<Address>]
[OpenApiMarker<RekiEventId>]
internal static class DependencyInjection
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
            options.AddSchemaTransformer<AddressSchemaTransformer>();
            options.AddSchemaTransformer<NullableChallengeSchemaTransformer>();
            options.AddOperationTransformer<OperationMetadataTransformer>();
            options.AddOperationTransformer<AuthOperationTransformer>();
            options.AddDocumentTransformer<TelegramMiniAppSecurityTransformer>();
        });
        services.AddControllers();
        services.AddSerialization();

        return services;
    }
}
