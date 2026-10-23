namespace Reki.Application.Posts.Responses.PostResult;

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

