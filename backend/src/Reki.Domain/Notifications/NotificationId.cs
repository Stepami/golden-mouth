using Vogen;

namespace Reki.Domain.Notifications;

[ValueObject<Guid>(comparison: ComparisonGeneration.Omit)]
public readonly partial struct NotificationId
{
    public static NotificationId New() => From(Guid.CreateVersion7());
}