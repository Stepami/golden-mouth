namespace Reki.Contracts.Posts;

/// <summary>Ход генерации комментария; только completed подтверждает сохранение.</summary>
public sealed record ReplyLiveEvent(
    string Status,
    string RequestId)
{
    public string? Text { get; init; }

    public PostDetailResponse? Post { get; init; }

    public int? EnergyLeft { get; init; }

    public long? Subscribers { get; init; }

    public string? Code { get; init; }
}