using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Reki.Infrastructure.AI;

/// <summary>Enriches the active GenAI span before the telemetry wrapper exports it.</summary>
internal sealed class LlmSpendChatClient(
    IChatClient innerClient,
    LlmSpendReader spendReader) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var activity = LlmCostPolicy.FindChatActivity();
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        if (activity?.GetTagItem("gen_ai.usage.cost.source") is null)
        {
            await spendReader.RecordAsync(response.ResponseId, activity, cancellationToken);
        }

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var activity = LlmCostPolicy.FindChatActivity();
        string? responseId = null;
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            responseId = update.ResponseId ?? responseId;
            yield return update;
        }

        // Streaming headers precede billing. Read actual spend after consuming the final chunk.
        await spendReader.RecordAsync(responseId, activity, cancellationToken);
    }
}
