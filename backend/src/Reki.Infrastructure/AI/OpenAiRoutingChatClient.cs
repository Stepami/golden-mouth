using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Reki.Infrastructure.AI;

internal class OpenAiRoutingChatClient(IOptions<LlmOptions> options) : RoutingChatClient
{
    private readonly OpenAIClient _openAiClient = new(
        new ApiKeyCredential(options.Value.ApiKey),
        new OpenAIClientOptions
        {
            ClientLoggingOptions = new ClientLoggingOptions { EnableLogging = false },
            Endpoint = new Uri(options.Value.BaseUrl),
            NetworkTimeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds),
            RetryPolicy = new ClientRetryPolicy(),
        });

    protected override ValueTask<IChatClient> SelectClientAsync(
        RoutingContext context,
        CancellationToken cancellationToken)
    {
        var client = _openAiClient
            .GetChatClient(options.Value.Model)
            .AsIChatClient();

        return ValueTask.FromResult(client);
    }
}