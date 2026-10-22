using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reki.Application.Exceptions;
using Reki.Application.Posts;
using Reki.Application.Posts.AI.Safety;

namespace Reki.Api;

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
        var problemDetails = CreateProblemDetails(exception, httpContext.Request.Path);
        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        if (exception is ResourceNotFoundException or
            RequestConflictException or
            MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } or
            ContentRejectedException or
            UnsafeGeneratedContentException or
            CrisisDetectedException or
            AiServiceUnavailableException)
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
        var problemDetails = exception switch
        {
            ResourceNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Type = "urn:reki:problem:resource-not-found",
                Detail = "The requested resource was not found.",
            },
            RequestConflictException or MongoWriteException { WriteError.Category: ServerErrorCategory.DuplicateKey } => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Request conflict",
                Type = "urn:reki:problem:request-conflict",
                Detail = "The request conflicts with the current resource state.",
            },
            ContentRejectedException or UnsafeGeneratedContentException => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Content rejected",
                Type = "urn:reki:problem:content-rejected",
                Detail = "Так не получится, попробуй по-другому.",
            },
            CrisisDetectedException crisis => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Support is available",
                Type = "urn:reki:problem:crisis",
                Detail = crisis.IsTeen
                    ? "Игра поставлена на паузу. Детский телефон доверия: 8-800-2000-122."
                    : "Игра поставлена на паузу. Поговори с близким человеком или обратись к психологу.",
            },
            AiServiceUnavailableException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "AI service unavailable",
                Type = "urn:reki:problem:ai-unavailable",
                Detail = "Не получилось получить безопасный ответ. Попробуй ещё раз позже.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected error",
                Type = "urn:reki:problem:unexpected-error",
                Detail = "An unexpected error occurred.",
            },
        };

        problemDetails.Extensions["failure"] = exception switch
        {
            ContentRejectedException rejected => new PublicationFailure("content_rejected", rejected.Category, PublicationSafetyPolicy.Version),
            UnsafeGeneratedContentException rejected => new PublicationFailure("unsafe_generated_content", rejected.Category, PublicationSafetyPolicy.Version),
            CrisisDetectedException => new PublicationFailure("crisis", "crisis", PublicationSafetyPolicy.Version),
            AiServiceUnavailableException => new PublicationFailure("technical_failure", "uncertain", PublicationSafetyPolicy.Version),
            RequestConflictException conflict when conflict.Message.Contains("limit", StringComparison.OrdinalIgnoreCase) => new PublicationFailure("limit_reached", "limit", PublicationSafetyPolicy.Version),
            _ => null,
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
