using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI.Post;

public sealed record GeneratedPostTurn(
    string StarName,
    StarId StarId,
    string Comment,
    string Reaction,
    double Brave,
    double Humor,
    double Topic,
    IReadOnlyList<StarReason> Reasons,
    bool OnTopic,
    string Tone = "support");
