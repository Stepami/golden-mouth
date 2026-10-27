using Reki.Application.Posts.Responses.PostDetail;

namespace Reki.Application.Posts.Responses.ReplyLiveEvent;

/// <summary>Ход генерации комментария; только completed подтверждает сохранение.</summary>
public sealed record ReplyLiveEvent(
    ReplyStatus Status,
    string RequestId,
    string? Text,
    PostDetailResponse? Post,
    int? EnergyLeft,
    long? Subscribers,
    string? Code);
