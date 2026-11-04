using Reki.Application.Posts.AI.Post;

namespace Reki.Application.Posts;

public sealed record PublicationPart(
    string? Text,
    GeneratedPostTurn? Comment,
    string? Reaction);
