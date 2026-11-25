namespace Reki.Application.Notifications;

public interface INotificationContentFactory<in TContext>
{
    Task<string> GetContent(TContext context);
}