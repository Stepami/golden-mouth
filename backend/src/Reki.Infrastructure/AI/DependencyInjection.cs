using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Posts.AI;
using Reki.Infrastructure.AI.Chat;
using Reki.Infrastructure.AI.Embedding;

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
            .AddSingleton<LlmSpendPolicy>()
            .AddSingleton<OpenAiRoutingChatClient>()
            .AddChatClient(sp => sp.GetRequiredService<OpenAiRoutingChatClient>())
            .UseLogging()
            .UseOpenTelemetry(
                sourceName: "Reki.GenAI",
                configure: telemetry => telemetry.EnableSensitiveData = false)
            .Use(innerClient => new DeepSeekThinkingChatClient(innerClient))
            .Use(client => new LlmSpendChatClient(client));
        return services;
    }

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
        services.AddSingleton(typeof(IRekiEmbeddingGenerator<>), typeof(RekiEmbeddingGenerator<>));

        return services;
    }
}
