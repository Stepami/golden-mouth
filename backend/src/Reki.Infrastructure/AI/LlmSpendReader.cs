using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Reki.Infrastructure.AI;

internal sealed partial class LlmSpendReader(
    IServiceScopeFactory scopeFactory,
    IOptions<LlmOptions> options,
    TimeProvider timeProvider,
    ILogger<LlmSpendReader> logger)
{
    public async Task RecordAsync(string? responseId, Activity? activity, CancellationToken cancellationToken)
    {
        if (activity?.IsAllDataRequested is not true || string.IsNullOrEmpty(responseId))
        {
            return;
        }

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(options.Value.SpendLookupTimeoutSeconds), timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<ILlmSpendApi>();
            for (var attempt = 0; attempt < 4; attempt++)
            {
                using var response = await api.GetLogsAsync(responseId, linked.Token);
                if (response.IsSuccessStatusCode)
                {
                    var logs = await response.Content.ReadFromJsonAsync(
                        LlmSpendJsonContext.Default.LlmSpendLogArray, linked.Token);
                    var matches = logs?.Where(log => log is not null && log.RequestId == responseId).ToArray();
                    if (matches is [var entry] && entry.Spend is { } spend && double.IsFinite(spend) && spend >= 0)
                    {
                        activity.SetTag("gen_ai.usage.cost", spend);
                        activity.SetTag("gen_ai.usage.cost.currency", "RUB");
                        activity.SetTag("gen_ai.usage.cost.source", "gateway");
                        activity.SetTag("gen_ai.usage.cost.method", "spend_logs");
                        activity.SetTag("gen_ai.usage.cost.lookup_status", "recorded");
                        return;
                    }

                    if (logs is not { Length: 0 })
                    {
                        break;
                    }
                }
                else
                {
                    if (response.StatusCode != HttpStatusCode.NotFound)
                    {
                        LogLookupRejected((int)response.StatusCode);
                        activity.SetTag("gen_ai.usage.cost.lookup_status", "unavailable");
                        return;
                    }
                }

                // Spend logs are written asynchronously; retry an empty result within the total budget.
                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250 * (1 << attempt)), timeProvider, linked.Token);
                }
            }

            activity.SetTag("gen_ai.usage.cost.lookup_status", "missing");
            LogSpendMissing();
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or OperationCanceledException or
                                         JsonException or BrokenCircuitException or TimeoutRejectedException)
        {
            activity.SetTag("gen_ai.usage.cost.lookup_status", "unavailable");

            // Never include the response body or exception message: spend logs can contain credentials/content.
            LogLookupFailed(exception.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway spend lookup returned HTTP {StatusCode}")]
    private partial void LogLookupRejected(int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway spend log is missing or invalid after bounded lookup")]
    private partial void LogSpendMissing();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway spend lookup unavailable ({FailureType})")]
    private partial void LogLookupFailed(string failureType);
}
