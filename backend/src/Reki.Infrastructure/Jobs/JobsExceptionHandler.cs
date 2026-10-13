using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Enums;
using TickerQ.Utilities.Interfaces;

namespace Reki.Infrastructure.Jobs;

public sealed partial class JobsExceptionHandler(ILogger<JobsExceptionHandler> logger) : ITickerExceptionHandler
{
    public Task HandleExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        LogJobFailed(exception, tickerId, tickerType);
        return Task.CompletedTask;
    }

    public Task HandleCanceledExceptionAsync(Exception exception, Guid tickerId, TickerType tickerType)
    {
        LogJobCanceled(tickerId, tickerType);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Job {TickerId} ({TickerType}) failed")]
    private partial void LogJobFailed(Exception exception, Guid tickerId, TickerType tickerType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Job {TickerId} ({TickerType}) canceled")]
    private partial void LogJobCanceled(Guid tickerId, TickerType tickerType);
}
