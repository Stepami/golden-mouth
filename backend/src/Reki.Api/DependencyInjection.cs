using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Reki.Api.Mapping;
using Reki.Api.OpenApi;
using Reki.Infrastructure.Serialization;
using TgMiniAppAuth;

namespace Reki.Api;

internal static partial class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer<NullableChallengeSchemaTransformer>();
            options.AddOperationTransformer<OperationMetadataTransformer>();
            options.AddDocumentTransformer<TelegramMiniAppSecurityTransformer>();
        });
        services
            .AddControllers(options => options.Filters.Add(
                new AuthorizeFilter(TgMiniAppAuthConstants.AuthenticationScheme)))
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
        services.AddSingleton<ApiMapper>();
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.TypeInfoResolverChain.Insert(0, ApiJsonSerializerContext.Default);
        options.TypeInfoResolverChain.Insert(0, ProblemDetailsJsonSerializerContext.Default);
        foreach (var converter in ApiJsonSerializerContext.Default.Options.Converters)
        {
            options.Converters.Add(converter);
        }
    }

    [JsonSerializable(typeof(ProblemDetails))]
    [JsonSerializable(typeof(HttpValidationProblemDetails))]
    [JsonSerializable(typeof(ValidationProblemDetails))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    internal sealed partial class ProblemDetailsJsonSerializerContext : JsonSerializerContext;
}
