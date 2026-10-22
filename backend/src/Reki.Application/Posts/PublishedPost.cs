using Reki.Application.Posts.AI;
using Reki.Domain.Posts;

namespace Reki.Application.Posts;

public sealed record PublishedPost(
    GamePost Post,
    string Hook,
    string StarName,
    string Reaction,
    IReadOnlyList<StarReason> Reasons);
