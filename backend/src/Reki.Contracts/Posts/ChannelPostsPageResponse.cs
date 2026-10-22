namespace Reki.Contracts.Posts;

/// <summary>A page of the player's channel posts.</summary>
public sealed record ChannelPostsPageResponse(IReadOnlyList<ChannelPostResponse> Items, int PageNumber, bool HasNext);
