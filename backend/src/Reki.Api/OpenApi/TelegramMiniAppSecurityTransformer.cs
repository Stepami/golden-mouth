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

        foreach (OpenApiOperation operation in document.Paths.Values.SelectMany(path =>
                     path.Operations?.Values.AsEnumerable() ?? Enumerable.Empty<OpenApiOperation>()))
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(TgMiniAppAuthConstants.AuthenticationScheme, document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
