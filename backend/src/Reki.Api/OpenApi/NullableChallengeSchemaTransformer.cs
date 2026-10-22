using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Reki.Application.Social.Responses;

namespace Reki.Api.OpenApi;

internal sealed class NullableChallengeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Ok<ChallengeResponse?> writes an empty 200 when no challenge is active.
        // Mark the named schema nullable rather than creating an anonymous union,
        // so NSwag can continue reusing the existing Application response DTO.
        if (context.JsonTypeInfo.Type == typeof(ChallengeResponse))
        {
            schema.Type |= JsonSchemaType.Null;
        }

        return Task.CompletedTask;
    }
}
