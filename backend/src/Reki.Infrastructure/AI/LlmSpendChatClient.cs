using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Reki.Infrastructure.AI;

/// <summary>Reads gateway spend once the streaming response has completed.</summary>
internal sealed partial class LlmSpendChatClient(
    IChatClient innerClient,
    ILlmSpendApi spendApi,
    IOptions<LlmOptions> llmOptions,
    ILogger<LlmSpendChatClient> logger) : DelegatingChatClient(innerClient)
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
            using var response = await spendApi.GetLogsAsync(
                responseId, $"Bearer {llmOptions.Value.ApiKey}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LlmSpendWriter.WriteLookupStatus(activity, "unavailable");
                LogLookupRejected((int)response.StatusCode);
                yield break;
            }

            var logs = await response.Content.ReadFromJsonAsync(
                LlmSpendJsonContext.Default.LlmSpendLogArray, cancellationToken);
            var matches = logs?.Where(log => log is not null && log.RequestId == responseId).ToArray();
            if (matches is [var entry] && entry.Spend is { } spend && double.IsFinite(spend) && spend >= 0)
            {
                LlmSpendWriter.Write(activity, spend, "spend_logs");
                LlmSpendWriter.WriteLookupStatus(activity, "recorded");
            }
            else
            {
                LlmSpendWriter.WriteLookupStatus(activity, "missing");
                LogSpendMissing();
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException or
                                         JsonException or BrokenCircuitException or TimeoutRejectedException)
        {
            LlmSpendWriter.WriteLookupStatus(activity, "unavailable");

            // Exception messages and response bodies can contain credentials or user content.
            LogLookupFailed(exception.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway spend lookup returned HTTP {StatusCode}")]
    private partial void LogLookupRejected(int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway spend log is missing or invalid")]
    private partial void LogSpendMissing();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway spend lookup unavailable ({FailureType})")]
    private partial void LogLookupFailed(string failureType);
}
