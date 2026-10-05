using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record FeedItemResponse(
    FeedItemKind Kind,
    string? Author,
    string? Avatar,
    string? Role,
    string? Ago,
    string Text,
    IReadOnlyList<ReactionResponse>? Reactions,
    string? Repost);

