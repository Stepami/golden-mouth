using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Telegram.Bot;

namespace Reki.Api.Serialization;

internal static class DependencyInjection
{
    public static IServiceCollection AddSerialization(this IServiceCollection services)
    {
        services.AddOptions<JsonSerializerOptions>()
            .Configure(ConfigureJson)
            .PostConfigure(options => options.MakeReadOnly());
        services.AddOptions<Microsoft.AspNetCore.Mvc.JsonOptions>()
            .Configure(options => ConfigureJson(options.JsonSerializerOptions));
        services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
        return services;
    }

    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.TypeInfoResolverChain.Clear();
        options.TypeInfoResolverChain.Add(ApiJsonSerializerContext.Default);
        options.TypeInfoResolverChain.Add(JsonBotSerializerContext.Default);
        options.Converters.Clear();
        foreach (var converter in ApiJsonSerializerContext.Default.Options.Converters)
        {
            options.Converters.Add(converter);
        }
    }
}
