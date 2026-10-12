using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Reki.Infrastructure.AI;

internal class OpenAiRoutingChatClient(
    IOptions<LlmOptions> options,
    LlmSpendPolicy costPolicy,
    IServiceScopeFactory serviceScopeFactory) : RoutingChatClient
{
    /// <summary>
    /// Обёрткам нужны метаданные модели до вызова SelectClientAsync.
    /// </summary>
    private readonly ChatClientMetadata _metadata = new(
        defaultModelId: options.Value.Model);

    private readonly OpenAIClient _openAiClient = new(
        new ApiKeyCredential(options.Value.ApiKey),
        CreateClientOptions(options.Value, costPolicy));

    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null && serviceType == typeof(ChatClientMetadata))
        {
            return _metadata;
        }

        return base.GetService(serviceType, serviceKey);
    }

    protected override ValueTask<IChatClient> SelectClientAsync(
        RoutingContext context,
        CancellationToken cancellationToken)
    {
        var client = _openAiClient
            .GetChatClient(options.Value.Model)
            .AsIChatClient()
            .AsBuilder()
            .Use(client => new LlmSpendChatClient(client, serviceScopeFactory))
            .Build();

        return ValueTask.FromResult(client);
    }

    private static OpenAIClientOptions CreateClientOptions(LlmOptions options, LlmSpendPolicy costPolicy)
    {
        var clientOptions = new OpenAIClientOptions
        {
            ClientLoggingOptions = new ClientLoggingOptions { EnableLogging = false },
            Endpoint = new Uri(options.BaseUrl),
            NetworkTimeout = TimeSpan.FromSeconds(20),
            RetryPolicy = new ClientRetryPolicy(),
        };

        // Per-call runs outside retries and records the final non-streaming response.
        clientOptions.AddPolicy(costPolicy, PipelinePosition.PerCall);
        return clientOptions;
    }
}
