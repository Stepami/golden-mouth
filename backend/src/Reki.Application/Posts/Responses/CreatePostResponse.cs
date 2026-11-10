using Reki.Application.Posts.Responses.PostDetail;
using Reki.Application.Posts.Responses.PostResult;
using Reki.Domain.Posts;

namespace Reki.Application.Posts.Responses;

public sealed record CreatePostResponse(PostDetailResponse Post, PostResultResponse Result, AuthorId PredictedAuthorId);
