namespace Reki.Contracts.Posts;

/// <summary>A page of world feed posts.</summary>
public sealed record FeedPageResponse(IReadOnlyList<FeedItemResponse> Items, int PageNumber, bool HasNext);
