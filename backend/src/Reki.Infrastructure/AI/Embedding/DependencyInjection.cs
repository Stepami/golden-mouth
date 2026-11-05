using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Reki.Infrastructure.AI.Embedding;

internal static class DependencyInjection
{
    public static IServiceCollection AddEmbeddingClients(this IServiceCollection services)
    {
        services
            .AddOptions<EmbeddingOptions>()
            .BindConfiguration(EmbeddingOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           uri.Scheme is "http" or "https",
                "Embedding:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        services.AddSingleton<RekiOpenAiEmbedder>();
        services.AddEmbeddingGenerator(sp => sp.GetRequiredService<RekiOpenAiEmbedder>().GetEmbeddingGenerator())
            .UseLogging()
            .UseOpenTelemetry(
                sourceName: "Reki.Embed",
                configure: telemetry => telemetry.EnableSensitiveData = false);

        return services;
    }
}
