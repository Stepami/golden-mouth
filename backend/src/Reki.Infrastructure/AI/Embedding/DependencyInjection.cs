using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

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

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            var client = new OpenAIClient(
                new ApiKeyCredential("local"),
                new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl) });
            return client.GetEmbeddingClient(options.Model).AsIEmbeddingGenerator();
        });

        return services;
    }
}
