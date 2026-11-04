using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Reki.Api.OpenApi;

internal sealed class OperationMetadataTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
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

        if (operation.Responses is { } responses &&
            responses.TryGetValue("200", out var response) &&
            response is { Content: { } content } &&
            content.TryGetValue("text/event-stream", out var stream))
        {
            operation.Description = operation.OperationId switch
            {
                "Posts_GeneratePublication" => "Server-sent events. Each data frame is a PublicationStatusLiveEvent JSON object. " +
                    "Do not automatically repeat this POST on disconnect; query the durable publication status instead.",
                "Posts_GenerateReply" => "Server-sent events. Each data frame is a ReplyGenerationLiveEvent JSON object. " +
                    "Retry a technical or network failure once with the same authorId and sourceCommentId. " +
                    "Do not retry unsafe_generated_content automatically. The player's saved comment remains committed.",
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

        return Task.CompletedTask;
    }
}
