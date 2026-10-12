using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Reki.Infrastructure.AI;

/// <summary>Requests streaming usage and records gateway cost from the final usage chunk.</summary>
internal sealed class LlmSpendChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var activity = LlmSpendWriter.FindChatActivity();
        await foreach (var update in base.GetStreamingResponseAsync(messages, WithUsageIncluded(options), cancellationToken))
        {
            if (update.RawRepresentation is StreamingChatCompletionUpdate { Usage: { } usage } &&
                usage.Patch.TryGetValue("$.cost"u8, out double cost))
            {
                LlmSpendWriter.Write(activity, cost, "stream_usage");
            }

            yield return update;
        }
    }

    private static ChatOptions WithUsageIncluded(ChatOptions? options)
    {
        var patched = options?.Clone() ?? new ChatOptions();
        var rawFactory = patched.RawRepresentationFactory;
        patched.RawRepresentationFactory = client =>
        {
            var completionOptions = rawFactory?.Invoke(client) as ChatCompletionOptions ?? new ChatCompletionOptions();
            completionOptions.Patch.Set("$.stream_options.include_usage"u8, true);
            return completionOptions;
        };
        return patched;
    }
}
