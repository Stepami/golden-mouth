using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses;

public sealed record QuotedPostResponse(PostId PostId, string? Author, string? Avatar, string? Text);
