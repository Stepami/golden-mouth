using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Reki.Api.Common.Errors;

internal static class InvalidModelStateResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (key, entry) in context.ModelState)
        {
            if (entry is { Errors.Count: > 0 })
            {
                errors.Add(key, entry.Errors.Select(error =>
                    string.IsNullOrEmpty(error.ErrorMessage) ? "The input was not valid." : error.ErrorMessage).ToArray());
            }
        }

        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var defaults = factory.CreateProblemDetails(
            context.HttpContext,
            StatusCodes.Status400BadRequest,
            "One or more validation errors occurred.");
        var problem = new HttpValidationProblemDetails(errors)
        {
            Status = defaults.Status,
            Title = defaults.Title,
            Type = defaults.Type,
            Detail = defaults.Detail,
            Instance = defaults.Instance,
            Extensions = defaults.Extensions,
        };
        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json", "application/problem+xml" },
        };
    }
}
