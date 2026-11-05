using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Reki.Application.Posts.AI;

namespace Reki.Infrastructure.AI.Chat;

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
            .UseOpenTelemetry(
                sourceName: "Reki.GenAI",
                configure: telemetry => telemetry.EnableSensitiveData = false)
            .Use(innerClient => new DeepSeekThinkingChatClient(innerClient))
            .Use(client => new LlmSpendChatClient(client));
        return services;
    }
}
