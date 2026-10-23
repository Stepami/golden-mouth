using Reki.Domain.Player;

namespace Reki.Application.Posts.PublishPostCommand;

public sealed record PostProgression(
    long SubscriberDelta,
    double Quality,
    bool Viral,
    bool Flop,
    IReadOnlyDictionary<SkillId, SkillProgress> Skills);
