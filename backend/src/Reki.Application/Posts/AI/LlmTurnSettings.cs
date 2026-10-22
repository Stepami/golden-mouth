using System.ComponentModel.DataAnnotations;

namespace Reki.Application.Posts.AI;

public sealed record LlmTurnSettings
{
    [Range(0, 2)]
    public float Temperature { get; init; } = 0.4f;
}
