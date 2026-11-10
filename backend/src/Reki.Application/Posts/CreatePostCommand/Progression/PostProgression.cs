using Reki.Domain.Player;

namespace Reki.Application.Posts.CreatePostCommand.Progression;

public sealed record PostProgression(
    long SubscriberDelta,
    double Quality,
    bool Viral,
    bool Flop,
    IReadOnlyDictionary<SkillId, SkillProgress> Skills);
