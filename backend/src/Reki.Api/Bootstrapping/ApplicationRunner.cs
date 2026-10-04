namespace Reki.Api.Bootstrapping;

internal sealed partial class ApplicationRunner(ILogger<ApplicationRunner> logger)
{
    public async Task RunAsync(string[] arguments)
    {
        try
        {
            await Bootstrap.RunAsync(arguments);
        }
        catch (Exception exception)
        {
            LogServiceStopped(exception);
        }
    }

    [LoggerMessage(
        EventId = 9000,
        Level = LogLevel.Critical,
        Message = "The service stopped unexpectedly")]
    private partial void LogServiceStopped(Exception exception);
}
