namespace Reki.Application.Common.AI;

public sealed record LlmTurnSettings(
    string Model,
    float Temperature,
    int MaxOutputTokens);