using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Reki.Infrastructure.AI;

/// <summary>Reads gateway spend once the streaming response has completed.</summary>
internal sealed partial class LlmSpendChatClient(
    IChatClient innerClient,
    IServiceScopeFactory serviceScopeFactory) : DelegatingChatClient(innerClient)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var activity = LlmSpendWriter.FindChatActivity();
        string? responseId = null;
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            responseId = update.ResponseId ?? responseId;
            yield return update;
        }

        if (activity?.IsAllDataRequested is not true || string.IsNullOrEmpty(responseId))
        {
            yield break;
        }

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            using var response = await scope.ServiceProvider.GetRequiredService<ILlmSpendApi>()
                .GetLogsAsync(
                    responseId,
                    cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                yield break;
            }

            var logs = await response.Content.ReadFromJsonAsync(
                LlmSpendJsonContext.Default.LlmSpendLogArray,
                cancellationToken);
            var matches = logs?.Where(log => log is not null && log.RequestId == responseId).ToArray();
            if (matches is [{ Spend: { } spend }] && double.IsFinite(spend) && spend >= 0)
            {
                LlmSpendWriter.Write(activity, spend, "spend_logs");
            }
        }
        catch (Exception)
        {
            // Cost telemetry must not fail an already completed model response.
        }
    }

    private sealed record LlmSpendLog(
        [property: JsonPropertyName("request_id")] string? RequestId,
        [property: JsonPropertyName("spend")] double? Spend);

    [JsonSerializable(typeof(LlmSpendLog[]))]
    private sealed partial class LlmSpendJsonContext : JsonSerializerContext;
}
