using Reki.Domain.Worlds;

namespace Reki.Application.Posts.AI;

internal sealed record StarPlayerPromptData(
    string WorldId,
    string WorldTitle,
    string WorldDescription,
    IReadOnlyList<WorldRule> WorldRules,
    string Drama,
    string ChannelVoice,
    string Address,
    string? StarId);
