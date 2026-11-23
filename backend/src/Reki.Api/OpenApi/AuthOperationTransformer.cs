using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Reki.Api.Requests.Auth;

namespace Reki.Api.OpenApi;

internal sealed class AuthOperationTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var requestType = context.Description.ActionDescriptor.Parameters
            .Select(parameter => parameter.ParameterType)
            .FirstOrDefault(type => type == typeof(TokenExchangeRequest) || type == typeof(RefreshTokenRequest) || type == typeof(LogoutRequest));
        if (requestType is not null && operation.RequestBody?.Content is { } requestContent &&
            requestContent.TryGetValue("application/x-www-form-urlencoded", out var form))
        {
            requestContent["application/x-www-form-urlencoded"] = new OpenApiMediaType((OpenApiMediaType)form)
            {
                Schema = await GetNamedSchemaAsync(context, requestType, cancellationToken),
            };
        }

        if (requestType == typeof(TokenExchangeRequest) || requestType == typeof(RefreshTokenRequest))
        {
            operation.Responses ??= new OpenApiResponses();
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "OK",
                Content = new Dictionary<string, IOpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = await GetNamedSchemaAsync(context, typeof(AuthTokenResponse), cancellationToken),
                    },
                },
            };
        }
    }

    private static async Task<OpenApiSchemaReference> GetNamedSchemaAsync(
        OpenApiOperationTransformerContext context,
        Type type,
        CancellationToken cancellationToken)
    {
        var document = context.Document ?? throw new InvalidOperationException("OpenAPI document is unavailable.");
        var components = document.Components ??= new OpenApiComponents();
        var schemas = components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        if (!schemas.ContainsKey(type.Name))
        {
            schemas[type.Name] = await context.GetOrCreateSchemaAsync(type, null, cancellationToken);
        }

        return new OpenApiSchemaReference(type.Name, document);
    }
}
