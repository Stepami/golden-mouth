using System.Text.Json;
using Microsoft.Extensions.AI;
using Reki.Application.Posts.AI;
using Reki.Domain.Worlds;

namespace Reki.Application.Posts.GeneratePublicationCommand;

internal static class StarToolCallParser
{
    private static readonly string[] CommentKeys =
    [
        "author",
        "star_id",
        "text",
        "hook",
        "tone",
        "brave",
        "humor",
        "topic",
        "why",
        "on_topic",
    ];

    internal interface IStarAction;

    public static IReadOnlyList<IStarAction> Parse(IEnumerable<FunctionCallContent> calls)
    {
        return calls.Select(Parse).ToArray();
    }

    private static IStarAction Parse(FunctionCallContent call)
    {
        var arguments = call.Arguments
                        ?? throw new InvalidOperationException("Tool arguments are required.");
        return call.Name switch
        {
            "react" => ParseReaction(arguments),
            "comment" => ParseComment(arguments),
            "repost" => ParseRepost(arguments),
            "start_event" => ParseEvent(arguments),
            _ => throw new InvalidOperationException("The model returned an unknown action."),
        };
    }

    private static StarReactionAction ParseReaction(IDictionary<string, object?> arguments)
    {
        EnsureExactKeys(arguments, ["emoji"]);
        return new StarReactionAction(GetString(arguments, "emoji", 8, allowEmpty: false));
    }

    private static StarCommentAction ParseComment(IDictionary<string, object?> arguments)
    {
        EnsureExactKeys(arguments, CommentKeys);
        return new StarCommentAction(
            GetString(arguments, "author", 40, allowEmpty: false),
            StarId.From(GetString(arguments, "star_id", 40, allowEmpty: false)),
            GetString(arguments, "text", 500, allowEmpty: false),
            GetString(arguments, "hook", 240, allowEmpty: false),
            GetTone(arguments),
            GetScore(arguments, "brave"),
            GetScore(arguments, "humor"),
            GetScore(arguments, "topic"),
            GetReasons(arguments, "why"),
            GetBoolean(arguments, "on_topic"));
    }

    private static string GetTone(IDictionary<string, object?> arguments)
    {
        var tone = GetString(arguments, "tone", 10, allowEmpty: false);
        return tone is "support" or "critique" or "hate"
            ? tone
            : throw new InvalidOperationException("The model returned an invalid comment tone.");
    }

    private static StarRepostAction ParseRepost(IDictionary<string, object?> arguments)
    {
        EnsureExactKeys(arguments, ["star_id", "text"]);
        return new StarRepostAction(
            StarId.From(GetString(arguments, "star_id", 40, allowEmpty: false)),
            GetString(arguments, "text", 500, allowEmpty: false));
    }

    private static StarEventAction ParseEvent(IDictionary<string, object?> arguments)
    {
        EnsureExactKeys(arguments, ["type", "text"]);
        var type = GetString(arguments, "type", 20, allowEmpty: false);
        if (type is not ("collab" or "rumor" or "chance" or "hate"))
        {
            throw new InvalidOperationException("The model returned an unknown event type.");
        }

        return new StarEventAction(type, GetString(arguments, "text", 1000, allowEmpty: false));
    }

    private static IReadOnlyList<StarReason> GetReasons(
        IDictionary<string, object?> arguments,
        string name)
    {
        if (!arguments.TryGetValue(name, out var value) ||
            value is not JsonElement { ValueKind: JsonValueKind.Array } element)
        {
            throw new InvalidOperationException("The model returned invalid reasons.");
        }

        var reasons = new List<StarReason>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("The model returned an invalid reason.");
            }

            var properties = item.EnumerateObject().ToArray();
            if (properties.Length != 2 ||
                properties.Any(property => property.Name is not ("icon" or "label")) ||
                !item.TryGetProperty("icon", out var iconElement) ||
                !item.TryGetProperty("label", out var labelElement))
            {
                throw new InvalidOperationException("The model returned an invalid reason shape.");
            }

            reasons.Add(new StarReason(
                GetJsonString(iconElement, 8, allowEmpty: false),
                GetJsonString(labelElement, 80, allowEmpty: false)));
        }

        if (reasons.Count is < 1 or > 3)
        {
            throw new InvalidOperationException("The model returned an invalid reason count.");
        }

        return reasons;
    }

    private static string GetString(
        IDictionary<string, object?> arguments,
        string name,
        int maxLength,
        bool allowEmpty)
    {
        if (!arguments.TryGetValue(name, out var value))
        {
            throw new InvalidOperationException("A required tool argument is missing.");
        }

        var text = value switch
        {
            string stringValue => stringValue,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null,
        };
        return ValidateString(text, maxLength, allowEmpty);
    }

    private static string GetJsonString(JsonElement element, int maxLength, bool allowEmpty)
    {
        var text = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        return ValidateString(text, maxLength, allowEmpty);
    }

    private static string ValidateString(string? text, int maxLength, bool allowEmpty)
    {
        if (text is null || text.Length > maxLength || (!allowEmpty && text.Length == 0))
        {
            throw new InvalidOperationException("The model returned an invalid string argument.");
        }

        return text;
    }

    private static double GetScore(IDictionary<string, object?> arguments, string name)
    {
        const double defaultScore = .01;
        if (!arguments.TryGetValue(name, out var value))
        {
            return defaultScore;
        }

        double? score = value switch
        {
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            JsonElement { ValueKind: JsonValueKind.Number } element when element.TryGetDouble(out var number) => number,
            _ => null,
        };
        if (score is null or < 0 or > 1 || double.IsNaN(score.Value))
        {
            return defaultScore;
        }

        return score.Value;
    }

    private static bool GetBoolean(IDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var value))
        {
            throw new InvalidOperationException("A required boolean is missing.");
        }

        return value switch
        {
            bool boolean => boolean,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            _ => throw new InvalidOperationException("The model returned an invalid boolean."),
        };
    }

    private static void EnsureExactKeys(
        IDictionary<string, object?> arguments,
        IReadOnlyCollection<string> expected)
    {
        if (arguments.Count != expected.Count || arguments.Keys.Any(key => !expected.Contains(key)))
        {
            throw new InvalidOperationException("The model returned unknown or missing tool arguments.");
        }
    }

    internal sealed record StarReactionAction(string Emoji) : IStarAction;

    internal sealed record StarCommentAction(
        string Author,
        StarId StarId,
        string Text,
        string Hook,
        string Tone,
        double Brave,
        double Humor,
        double Topic,
        IReadOnlyList<StarReason> Reasons,
        bool OnTopic) : IStarAction;

    internal sealed record StarRepostAction(StarId StarId, string Text) : IStarAction;

    internal sealed record StarEventAction(string Type, string Text) : IStarAction;
}
