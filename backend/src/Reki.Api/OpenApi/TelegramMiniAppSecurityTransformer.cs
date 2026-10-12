using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using TgMiniAppAuth;

namespace Reki.Api.OpenApi;

internal sealed class TelegramMiniAppSecurityTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[TgMiniAppAuthConstants.AuthenticationScheme] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = ParameterLocation.Header,
                Description = "Enter TMiniApp followed by a space and the raw Telegram initData.",
            };

        var anonymousOperations = context.DescriptionGroups
            .SelectMany(group => group.Items)
            .Where(description => description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            .Select(description => description.ActionDescriptor.RouteValues)
            .Select(values => $"{values["controller"]}_{values["action"]}")
            .ToHashSet(StringComparer.Ordinal);
        foreach (var operation in document.Paths.Values.SelectMany(path =>
                     path.Operations?.Values.AsEnumerable() ?? Enumerable.Empty<OpenApiOperation>()))
        {
            if (operation.OperationId is { } operationId && anonymousOperations.Contains(operationId))
            {
                continue;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(TgMiniAppAuthConstants.AuthenticationScheme, document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
