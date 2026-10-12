using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Globalization;

namespace Reki.Infrastructure.AI;

/// <summary>Enriches the existing GenAI span with gateway-reported ruble costs.</summary>
internal sealed class LlmCostPolicy : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var activity = FindChatActivity();
        ProcessNext(message, pipeline, currentIndex);
        RecordCost(message.Response, activity);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        var activity = FindChatActivity();
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
        RecordCost(message.Response, activity);
    }

    private static Activity? FindChatActivity()
    {
        var activity = Activity.Current;
        while (activity is not null && activity.Source.Name != "Reki.GenAI")
        {
            activity = activity.Parent;
        }

        return activity;
    }

    private static double? ReadCost(PipelineResponseHeaders headers, string name)
    {
        return headers.TryGetValue(name, out var value) &&
               double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cost) &&
               double.IsFinite(cost) && cost >= 0
            ? cost
            : null;
    }

    private static void RecordCost(PipelineResponse? response, Activity? activity)
    {
        if (response is null || response.Status is < 200 or >= 300)
        {
            return;
        }

        var total = ReadCost(response.Headers, "X-Litellm-Response-Cost");
        if (total is null)
        {
            return;
        }

        activity?.SetTag("gen_ai.usage.cost", total.Value);
        activity?.SetTag("gen_ai.usage.cost.currency", "RUB");
        activity?.SetTag("gen_ai.usage.cost.source", "gateway");
        if (ReadCost(response.Headers, "X-Litellm-Response-Cost-Input") is { } input)
        {
            activity?.SetTag("gen_ai.usage.cost.input", input);
        }

        if (ReadCost(response.Headers, "X-Litellm-Response-Cost-Output") is { } output)
        {
            activity?.SetTag("gen_ai.usage.cost.output", output);
        }
    }
}
