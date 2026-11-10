using System.Diagnostics;

namespace Reki.Infrastructure.AI.Chat;

/// <summary>Writes discovered gateway costs to the existing GenAI span.</summary>
internal static class LlmSpendWriter
{
    public static Activity? FindChatActivity()
    {
        var activity = Activity.Current;
        while (activity is not null && activity.Source.Name != "Reki.GenAI")
        {
            activity = activity.Parent;
        }

        return activity;
    }

    public static void Write(Activity? activity, double cost, string method, double? input = null, double? output = null)
    {
        if (activity?.IsAllDataRequested is not true || !double.IsFinite(cost) || cost < 0)
        {
            return;
        }

        activity.SetTag("gen_ai.usage.cost", cost);
        activity.SetTag("gen_ai.usage.cost.currency", "RUB");
        activity.SetTag("gen_ai.usage.cost.source", "gateway");
        activity.SetTag("gen_ai.usage.cost.method", method);
        if (input is { } inputCost && double.IsFinite(inputCost) && inputCost >= 0)
        {
            activity.SetTag("gen_ai.usage.cost.input", inputCost);
        }

        if (output is { } outputCost && double.IsFinite(outputCost) && outputCost >= 0)
        {
            activity.SetTag("gen_ai.usage.cost.output", outputCost);
        }
    }
}
