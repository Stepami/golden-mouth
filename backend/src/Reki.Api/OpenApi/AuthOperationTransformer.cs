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
        var requestType = operation.OperationId switch
        {
            "Auth_Refresh" => typeof(RefreshTokenRequest),
            "Auth_Logout" => typeof(LogoutRequest),
            "ExternalTelegramAuth_ExchangeMiniApp" or "ExternalTelegramAuth_ExchangeTelegramLogin" => typeof(TokenExchangeRequest),
            "ExternalVkAuth_ExchangeVk" or "ExternalYandexAuth_ExchangeYandex" => typeof(TokenExchangeRequest),
            _ => null,
        };
        if (requestType is not null &&
            operation.RequestBody?.Content?.TryGetValue("application/x-www-form-urlencoded", out var form) == true)
        {
            form.Schema = await GetNamedSchemaAsync(context, requestType, cancellationToken);
        }

        if (operation.OperationId is "Auth_Refresh" or "ExternalTelegramAuth_ExchangeMiniApp" or "ExternalTelegramAuth_ExchangeTelegramLogin" or "ExternalVkAuth_ExchangeVk" or "ExternalYandexAuth_ExchangeYandex")
        {
            operation.Responses ??= new OpenApiResponses();
            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "OK",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new()
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
