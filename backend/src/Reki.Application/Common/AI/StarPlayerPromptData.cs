using Reki.Domain.Worlds;

namespace Reki.Application.Common.AI;

internal sealed record StarPlayerPromptData(
    string WorldId,
    string WorldTitle,
    string WorldDescription,
    IReadOnlyList<WorldRule> WorldRules,
    string Drama,
    string ChannelVoice,
    string Address,
    string? StarId,
    IReadOnlyList<string> Memory);
