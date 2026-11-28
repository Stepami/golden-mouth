using Reki.Domain.Player;
using Reki.Domain.Worlds;

namespace Reki.Domain.Posts;

public sealed record PostProgression(
    long SubscriberDelta,
    double Quality,
    PostViralityStatus Virality,
    IReadOnlyDictionary<SkillId, SkillProgress> Skills,
    IReadOnlyDictionary<SkillId, bool> LevelChange,
    long Views,
    long Likes,
    StarId Commenter,
    PostScore Score,
    int Multiplier = 1,
    int ExtraViews = 0);
