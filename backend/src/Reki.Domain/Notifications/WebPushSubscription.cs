namespace Reki.Domain.Notifications;

public sealed record WebPushSubscription(string Endpoint, string P256dh, string Auth);
