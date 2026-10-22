using Reki.Domain.Social;

namespace Reki.Application.Posts.AddReplyCommand;

public sealed record GeneratedReplyTurn(
    StarId StarId,
    string Text,
    string Tone,
    string? Emoji,
    StarId? AdditionalStarId,
    string? AdditionalText,
    string? AdditionalTone);