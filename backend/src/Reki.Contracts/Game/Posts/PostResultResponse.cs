using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reki.Contracts.Game;

public sealed record PostResultResponse(
    string PostId,
    long Delta,
    string Headline,
    IReadOnlyList<WhyResponse> Whys,
    IReadOnlyList<RibbonResponse> Ribbons,
    BestCommentResponse? BestComment,
    bool Viral,
    bool Flop,
    long Views,
    string? Hook);

