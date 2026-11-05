using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using Reki.Application.Posts.AI;

namespace Reki.Infrastructure.AI;

internal static class DependencyInjection
{
    public static IServiceCollection AddChatClients(this IServiceCollection services)
    {
        services
            .AddOptions<LlmOptions>()
            .BindConfiguration(LlmOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) &&
                           uri.Scheme is "http" or "https",
                "Llm:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        services
            .AddOptions<LlmTurnSettings>()
            .BindConfiguration(LlmOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddOptions<EmbeddingOptions>()
            .BindConfiguration(EmbeddingOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https", "Embedding:BaseUrl must be an absolute HTTP or HTTPS URI.")
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            var client = new OpenAIClient(
                new ApiKeyCredential("local"),
                new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl) });
            return client.GetEmbeddingClient(options.Model).AsIEmbeddingGenerator();
        });

        services
            .AddOptions<NliOptions>()
            .BindConfiguration(NliOptions.SectionName)
            .Validate(
                options => !options.Enabled ||
                           (File.Exists(options.ModelPath) && File.Exists(options.VocabPath)),
                "Nli model and vocabulary files must exist when enabled.")
            .ValidateOnStart();
        services.AddSingleton<LocalNliScorer>();
        services.AddSingleton<INliScorer>(sp => sp.GetRequiredService<LocalNliScorer>());
        services.AddHealthChecks().AddCheck<LocalNliScorer>("nli", null, ["ready"]);

        services.AddHttpClient<ITextSignalsScorer, InfinityClassifierClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value.BaseUrl);
        });

        services
            .AddSingleton<LlmSpendPolicy>()
            .AddSingleton<OpenAiRoutingChatClient>()
            .AddChatClient(sp => sp.GetRequiredService<OpenAiRoutingChatClient>())
            .UseOpenTelemetry(
                sourceName: "Reki.GenAI",
                configure: telemetry => telemetry.EnableSensitiveData = false)
            .Use(innerClient => new DeepSeekThinkingChatClient(innerClient))
            .Use(client => new LlmSpendChatClient(client));
        return services;
    }
}
