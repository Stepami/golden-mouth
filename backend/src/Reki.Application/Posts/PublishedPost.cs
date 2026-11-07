using Reki.Application.Posts.AI.Post;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record PublishedPost(
    GamePost Post,
    string StarName,
    string Reaction,
    IReadOnlyList<StarReason> Reasons);
