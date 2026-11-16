using Reki.Domain.Player;

namespace Reki.Domain.Notifications;

public sealed class Notification(
    NotificationId id,
    PlayerId playerId,
    NotificationStatus status,
    DateTimeOffset sendAt,
    string content,
    int version = 0)
{
    public const string DefaultContent = "Пора попадать в РЕКИ! Заходи в игру и пиши посты - тебя ждёт успех!";

    public NotificationId Id { get; private set; } = id;
    public PlayerId PlayerId { get; private set; } = playerId;
    public NotificationStatus Status { get; private set; } = status;
    public DateTimeOffset SendAt { get; private set; } = sendAt;
    public string Content { get; private set; } = content;
    public int Version { get; private set; } = version;

    public static Notification New(PlayerId playerId, DateTimeOffset sendAt, string content = DefaultContent) =>
        new(NotificationId.From(sendAt), playerId, NotificationStatus.Pending, sendAt, content);

    public void Reschedule(DateTimeOffset sendAt)
    {
        if (SendAt != sendAt)
        {
            SendAt = sendAt;
            Version++;
        }
    }

    public void MarkSent()
    {
        if (Status != NotificationStatus.Sent)
        {
            Status = NotificationStatus.Sent;
            Version++;
        }
    }
}
