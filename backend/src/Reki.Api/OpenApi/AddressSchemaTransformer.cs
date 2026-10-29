using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Reki.Domain.Player;

namespace Reki.Api.OpenApi;

internal sealed class AddressSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var isNullable = context.JsonTypeInfo.Type == typeof(Address?);
        if (context.JsonTypeInfo.Type == typeof(Address) || isNullable)
        {
            var values = new[] { Address.She, Address.He, Address.Nick }
                .Select(value => JsonSerializer.SerializeToNode(value, context.JsonTypeInfo.Options)
                    ?? throw new InvalidOperationException("Address must serialize to a JSON value."))
                .ToList();
            schema.Items = null;
            if (isNullable)
            {
                schema.Type = null;
                schema.Enum = null;
                schema.OneOf =
                [
                    new OpenApiSchema { Type = JsonSchemaType.Null },
                    new OpenApiSchema { Type = JsonSchemaType.String, Enum = values },
                ];
            }
            else
            {
                schema.Type = JsonSchemaType.String;
                schema.Enum = values;
            }
        }

        return Task.CompletedTask;
    }
}
