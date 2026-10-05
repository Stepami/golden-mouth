using Reki.Application.Common.AI;

namespace Reki.Application.Posts;

public sealed record GeneratedPostTurn(
    string StarName,
    string StarId,
    string Comment,
    string Hook,
    string Reaction,
    double Brave,
    double Humor,
    double Topic,
    IReadOnlyList<StarReason> Reasons,
    bool OnTopic);