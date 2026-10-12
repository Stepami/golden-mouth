using Microsoft.Extensions.AI;
using Reki.Application.Common.Exceptions;
using Reki.Application.Common.Safety;
using Reki.Domain.Social;

namespace Reki.Application.Posts;

public static class ReplyTurnPolicy
{
    private static readonly string[] Keys =
        ["star_id", "text", "tone", "emoji", "additional_star_id", "additional_text", "additional_tone"];

    public static GeneratedReplyTurn Parse(IReadOnlyList<FunctionCallContent> calls, StarReplyContext context)
    {
        if (calls is not [var call] || call.Name != "thread_reply" ||
            call.Arguments is not { } args || args.Count != Keys.Length || args.Keys.Any(key => !Keys.Contains(key)))
        {
            throw new AiServiceUnavailableException("The model returned an invalid thread turn.");
        }

        var star = Read(args, "star_id", 64, false);
        var text = Read(args, "text", 500, false);
        var tone = Read(args, "tone", 10, false);
        var emoji = Read(args, "emoji", 8, true);
        var other = Read(args, "additional_star_id", 64, true);
        var otherText = Read(args, "additional_text", 500, true);
        var otherTone = Read(args, "additional_tone", 10, true);
        if (star != context.TargetStarId.Value || !AllowedTone(tone, context.HateEnabled) ||
            (other.Length == 0 && (otherText.Length != 0 || otherTone.Length != 0)) ||
            (other.Length != 0 && (other == star || !context.Agent.World.Stars.Any(item => item.Id.Value == other) ||
                                   otherText.Length == 0 || !AllowedTone(otherTone, context.HateEnabled))))
        {
            throw new AiServiceUnavailableException("The model returned a forbidden identity or tone.");
        }

        return new GeneratedReplyTurn(context.TargetStarId, text, tone,  emoji.Length == 0 ? null : emoji, other.Length == 0 ? null : StarId.From(other),  otherText.Length == 0 ? null : otherText, otherTone.Length == 0 ? null : otherTone);
    }

    private static bool AllowedTone(string tone, bool hateEnabled) =>
        tone is "support" or "critique" || (tone == "hate" && hateEnabled);

    private static string Read(IDictionary<string, object?> args, string key, int maxLength, bool empty)
    {
        var value = args[key];
        var text = value switch
        {
            string raw => raw,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } json => json.GetString(),
            _ => null,
        };
        if (text is null || text.Length > maxLength || (!empty && string.IsNullOrWhiteSpace(text)) ||
            (text.Length > 0 && string.IsNullOrWhiteSpace(text)) || !ContentSafetyPolicy.IsOutputAllowed(text))
        {
            throw new AiServiceUnavailableException("The model returned unsafe or invalid thread content.");
        }

        return text;
    }
}