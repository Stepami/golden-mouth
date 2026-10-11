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
            stream.Schema = null;
            content.Remove("application/json");
        }

        return Task.CompletedTask;
    }
}
