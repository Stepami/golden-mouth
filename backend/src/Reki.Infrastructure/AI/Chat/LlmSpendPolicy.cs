using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Globalization;

namespace Reki.Infrastructure.AI.Chat;

/// <summary>Enriches the existing GenAI span with gateway-reported ruble costs.</summary>
internal sealed class LlmSpendPolicy : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var activity = LlmSpendWriter.FindChatActivity();
        ProcessNext(message, pipeline, currentIndex);
        RecordCost(message.Response, activity);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var activity = LlmSpendWriter.FindChatActivity();
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        RecordCost(message.Response, activity);
    }

    private static double? ReadCost(PipelineResponseHeaders headers, string name)
    {
        return headers.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cost) && double.IsFinite(cost) && cost >= 0 ? cost : null;
    }

    private static void RecordCost(PipelineResponse? response, Activity? activity)
    {
        if (response is null || response.Status is < 200 or >= 300)
        {
            return;
        }

        // Streaming headers are sent before actual spend is known; the completed response uses spend logs.
        if (response.Headers.TryGetValue("Content-Type", out var contentType) &&
            contentType?.StartsWith("text/event-stream", StringComparison.OrdinalIgnoreCase) is true)
        {
            return;
        }

        var total = ReadCost(response.Headers, "X-Litellm-Response-Cost");
        if (total is null)
        {
            return;
        }

        LlmSpendWriter.Write(
            activity,
            total.Value,
            "headers",
            ReadCost(response.Headers, "X-Litellm-Response-Cost-Input"),
            ReadCost(response.Headers, "X-Litellm-Response-Cost-Output"));
    }
}
