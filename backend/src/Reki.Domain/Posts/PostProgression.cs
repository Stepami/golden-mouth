using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Domain.Posts;

public sealed record PostProgression(
    long SubscriberDelta,
    double Quality,
    bool Viral,
    bool Flop,
    IReadOnlyDictionary<SkillId, SkillProgress> Skills,
    long Views,
    long Likes,
    StarId Commenter,
    PostScore Score);
