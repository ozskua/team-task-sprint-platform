using Microsoft.AspNetCore.Mvc;
using SprintFlow.Web.Server.Common;

namespace SprintFlow.Web.Server.Middleware;

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
            logger.LogWarning("Sign-in attempt rejected.");

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
