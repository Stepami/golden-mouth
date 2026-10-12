namespace Reki.Contracts.Posts;

/// <summary>Ход генерации комментария; только completed подтверждает сохранение.</summary>
public sealed record ReplyLiveEvent(
    ReplyStatus Status,
    string RequestId,
    string? Text,
    PostDetailResponse? Post,
    int? EnergyLeft,
    long? Subscribers,
    string? Code);
