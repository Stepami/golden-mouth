using Reki.Domain.Social;

namespace Reki.Application.Posts.AI;

public sealed record GeneratedPostTurn(
    string StarName,
    StarId StarId,
    string Comment,
    string Hook,
    string Reaction,
    double Brave,
    double Humor,
    double Topic,
    IReadOnlyList<StarReason> Reasons,
    bool OnTopic,
    string Tone = "support");
