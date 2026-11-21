using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ServiceFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}. TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            traceId);

        var (statusCode, title, detail) = MapException(exception);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? detail : title,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"] = traceId
            }
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),
            InvalidOperationException => (
                StatusCodes.Status409Conflict,
                "Operation conflict",
                exception.Message),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Authentication is required."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                exception.Message)
        };
    }
}
