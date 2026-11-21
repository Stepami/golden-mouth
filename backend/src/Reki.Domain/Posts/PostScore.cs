using Reki.Domain.Player;

namespace Reki.Domain.Posts;

public sealed record PostScore(
    double Brave,
    double Humor,
    double Topic,
    bool PositiveEmotion,
    string? FlopReason)
{
    public double Quality => Math.Clamp((Brave + Humor + Topic) / 3, 0, 1);

    public bool IsFlop => FlopReason is not null;

    public SkillId GetMainSkill()
    {
        if (Brave >= Humor && Brave >= Topic)
        {
            return SkillId.Brave;
        }

        return Humor >= Topic ? SkillId.Humor : SkillId.Topic;
    }
}
