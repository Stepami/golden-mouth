using System.ComponentModel.DataAnnotations;

namespace Reki.Application.Common.AI;

public sealed record LlmTurnSettings
{
    [Range(0, 2)]
    public float Temperature { get; init; } = 0.4f;

    [Range(1, int.MaxValue)]
    public int MaxOutputTokens { get; init; } = 700;
}
