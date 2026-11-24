using Reki.Domain.Player;

namespace Reki.Domain.Notifications;

public sealed class Notification(
    NotificationId id,
    PlayerId playerId,
    NotificationChannelType channelType,
    NotificationStatus status,
    DateTimeOffset sendAt,
    string content,
    int version = 0,
    NotificationKind kind = NotificationKind.Regular)
{
    public const string DefaultContent = "Пора попадать в РЕКИ! Заходи в игру и пиши посты - тебя ждёт успех!";

    public NotificationId Id { get; private set; } = id;
    public PlayerId PlayerId { get; private set; } = playerId;
    public NotificationChannelType ChannelType { get; private set; } = channelType;
    public NotificationKind Kind { get; private set; } = kind;
    public NotificationStatus Status { get; private set; } = status;
    public DateTimeOffset SendAt { get; private set; } = sendAt;
    public string Content { get; private set; } = content;
    public int Version { get; private set; } = version;

    public static Notification New(
        PlayerId playerId,
        NotificationChannelType channelType,
        DateTimeOffset sendAt,
        string content = DefaultContent,
        NotificationKind kind = NotificationKind.Regular) =>
        new(NotificationId.From(sendAt), playerId, channelType, NotificationStatus.Pending, sendAt, content, kind: kind);

    public void Reschedule(DateTimeOffset sendAt)
    {
        if (SendAt != sendAt)
        {
            SendAt = sendAt;
            Version++;
        }
    }

    public void Complete(NotificationStatus result)
    {
        if (Status != result)
        {
            Status = result;
            Version++;
        }
    }
}
