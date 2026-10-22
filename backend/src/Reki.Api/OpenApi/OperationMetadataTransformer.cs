using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using PublicationStatusLiveEvent = Reki.Application.Posts.Responses.PublicationStatusLiveEvent.PublicationStatusLiveEvent;
using ReplyLiveEvent = Reki.Application.Posts.Responses.ReplyLiveEvent.ReplyLiveEvent;

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

        return Task.CompletedTask;
    }
}
