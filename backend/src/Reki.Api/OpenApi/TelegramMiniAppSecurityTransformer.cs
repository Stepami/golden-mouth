using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Reki.Api.Auth;
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
        document.Components.SecuritySchemes["AppBearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };
        document.Components.SecuritySchemes["TelegramLogin"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };

        var endpointSecurity = context.DescriptionGroups
            .SelectMany(group => group.Items)
            .ToDictionary(
                description => $"{description.ActionDescriptor.RouteValues["controller"]}_{description.ActionDescriptor.RouteValues["action"]}",
                description =>
                {
                    var metadata = description.ActionDescriptor.EndpointMetadata;
                    if (metadata.OfType<IAllowAnonymous>().Any())
                    {
                        return string.Empty;
                    }

                    var authorization = metadata.OfType<IAuthorizeData>().ToArray();
                    if (authorization.Any(item => item.Policy == AuthConstants.AppJwtPolicy))
                    {
                        return "AppBearer";
                    }

                    if (authorization.Any(item => item.AuthenticationSchemes == AuthConstants.TelegramLoginScheme))
                    {
                        return AuthConstants.TelegramLoginScheme;
                    }

                    return authorization.Any(item => item.AuthenticationSchemes == TgMiniAppAuthConstants.AuthenticationScheme)
                        ? TgMiniAppAuthConstants.AuthenticationScheme
                        : string.Empty;
                },
                StringComparer.Ordinal);
        foreach (var operation in document.Paths.Values.SelectMany(path =>
                     path.Operations?.Values.AsEnumerable() ?? Enumerable.Empty<OpenApiOperation>()))
        {
            if (operation.OperationId is not { } operationId ||
                !endpointSecurity.TryGetValue(operationId, out var scheme) || scheme.Length == 0)
            {
                continue;
            }

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(scheme, document)] = [],
            });
        }

        return Task.CompletedTask;
    }
}
