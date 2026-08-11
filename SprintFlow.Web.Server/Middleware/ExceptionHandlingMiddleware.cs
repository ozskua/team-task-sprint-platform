using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common.Exceptions;

namespace SprintFlow.Web.Server.Middleware;

/// <summary>
/// Turns exceptions into RFC 7807 <see cref="ProblemDetails"/>. Nothing about the server's internals
/// reaches the client: known failures get their intended status and message, and anything unexpected
/// becomes a plain 500 while the real detail goes to the log.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AuthenticationFailedException exception)
        {
            // Expected outcome of a bad sign-in, not a fault — logged at a level that will not fill
            // the log with noise, and without the submitted address.
            logger.LogInformation("Sign-in attempt rejected.");

            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}.", context.Request.Path);

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred. Please try again.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            // The response is already on the wire; overwriting it would corrupt it.
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem);
    }
}
