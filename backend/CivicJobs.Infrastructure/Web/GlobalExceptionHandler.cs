using CivicJobs.Application.Exceptions;
using CivicJobs.Core.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CivicJobs.Infrastructure.Web;

/// <summary>
/// Maps every exception type listed in CLAUDE.md to an HTTP status with ProblemDetails, so controllers never catch.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(error => ToCamelCase(error.PropertyName))
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            },
            DomainValidationException => Problem(StatusCodes.Status400BadRequest, "Invalid data.", exception.Message),
            InvalidStatusTransitionException => Problem(StatusCodes.Status409Conflict, "Action not allowed in the current status.", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict.", exception.Message),
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Not found.", exception.Message),
            InvalidCredentialsException or UnauthorizedException => Problem(StatusCodes.Status401Unauthorized, "Unauthorized.", exception.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "Something went wrong on our side. Try again in a moment.")
        };

        if (problemDetails.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };

    // Matches the camelCase JSON field names, so the frontend can show each error next to its field.
    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName) ? propertyName : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
