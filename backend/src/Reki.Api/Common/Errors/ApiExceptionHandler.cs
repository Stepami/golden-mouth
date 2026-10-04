using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Reki.Application.Common.Exceptions;

namespace Reki.Api.Common.Errors;

internal sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails = CreateProblemDetails(exception, httpContext.Request.Path);
        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        if (exception is ResourceNotFoundException or RequestConflictException)
        {
            LogMappedException(
                exception.GetType().Name,
                httpContext.Response.StatusCode);
        }
        else
        {
            LogUnhandledException(exception);
        }

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
        };

        return await problemDetailsService.TryWriteAsync(problemDetailsContext);
    }

    private static ProblemDetails CreateProblemDetails(
        Exception exception,
        PathString instance)
    {
        ProblemDetails problemDetails = exception switch
        {
            ResourceNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Type = "urn:reki:problem:resource-not-found",
                Detail = "The requested resource was not found.",
            },
            RequestConflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Request conflict",
                Type = "urn:reki:problem:request-conflict",
                Detail = "The request conflicts with the current resource state.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected error",
                Type = "urn:reki:problem:unexpected-error",
                Detail = "An unexpected error occurred.",
            },
        };

        problemDetails.Instance = instance;
        return problemDetails;
    }

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Warning,
        Message = "Mapped exception {ExceptionType} to HTTP status {StatusCode}")]
    private partial void LogMappedException(
        string exceptionType,
        int statusCode);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Error,
        Message = "An unhandled exception reached the HTTP pipeline")]
    private partial void LogUnhandledException(Exception exception);
}
