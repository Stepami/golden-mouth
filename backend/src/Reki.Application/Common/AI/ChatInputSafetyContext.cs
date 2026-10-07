using Reki.Domain.Player;

namespace Reki.Application.Common.AI;

public sealed record ChatInputSafetyContext(PlayerProfile Player, IReadOnlyList<string> Texts)
{
    public const string OptionKey = "reki.input_safety_context";
}
