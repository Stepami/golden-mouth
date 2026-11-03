using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Reki.Api.Requests.Auth;

namespace Reki.Api.OpenApi;

internal sealed class OperationMetadataTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var routeValues = context.Description.ActionDescriptor.RouteValues;

        if (routeValues.TryGetValue("controller", out var controller) &&
            routeValues.TryGetValue("action", out var action))
        {
            operation.OperationId = $"{controller}_{action}";
        }

        var authRequestType = operation.OperationId switch
        {
            "Auth_Refresh" => typeof(RefreshTokenRequest),
            "Auth_Logout" => typeof(LogoutRequest),
            "ExternalTelegramAuth_ExchangeMiniApp" or "ExternalTelegramAuth_ExchangeTelegramLogin" => typeof(TokenExchangeRequest),
            _ => null,
        };
        if (authRequestType is not null &&
            operation.RequestBody?.Content?.TryGetValue("application/x-www-form-urlencoded", out var form) == true)
        {
            form.Schema = await GetNamedSchemaAsync(context, authRequestType, cancellationToken);
        }

        if (operation.OperationId is "Auth_Refresh" or "ExternalTelegramAuth_ExchangeMiniApp" or "ExternalTelegramAuth_ExchangeTelegramLogin")
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

        if (operation.Responses is { } responses &&
            responses.TryGetValue("200", out var response) &&
            response is { Content: { } content } &&
            content.TryGetValue("text/event-stream", out var stream))
        {
            operation.Description = operation.OperationId switch
            {
                "Posts_GeneratePublication" => "Server-sent events. Each data frame is a PublicationStatusLiveEvent JSON object. " +
                    "Do not automatically repeat this POST on disconnect; query the durable publication status instead.",
                "Posts_ReplyToPost" => "Server-sent events. Each data frame is a ReplyLiveEvent JSON object. " +
                    "After disconnect, check the saved post for the requestId before retrying.",
                _ => operation.Description,
            };

            // NSwag needs a binary schema to retain the response stream instead of
            // returning an empty FileResponse and disposing the HTTP response.
            stream.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };
            content.Remove("application/json");
        }

        if (operation.OperationId == "Social_GetActiveChallenge" &&
            operation.Responses?["200"] is { } challengeResponse)
        {
            challengeResponse.Description = "Active challenge, or an empty HTTP 200 body when no challenge exists. " +
                "Clients normalize the empty body to null.";
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
