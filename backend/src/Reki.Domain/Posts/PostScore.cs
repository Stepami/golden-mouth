using Reki.Domain.Player;

namespace Reki.Domain.Posts;

public sealed record PostScore(
    double Brave,
    double Humor,
    double Topic,
    bool PositiveEmotion,
    string? FlopReason)
{
    public SkillId GetMainSkill()
    {
        if (Brave >= Humor && Brave >= Topic)
        {
            return SkillId.Brave;
        }

        return Humor >= Topic ? SkillId.Humor : SkillId.Topic;
    }
}
