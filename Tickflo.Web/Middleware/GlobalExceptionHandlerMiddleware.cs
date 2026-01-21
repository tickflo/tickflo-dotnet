namespace Tickflo.Web.Middleware;

using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;

/// <summary>
/// Middleware for handling unhandled exceptions globally.
/// Logs errors and routes to the error page while preserving exception context.
/// </summary>
public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUserService, IUserRepository userRepository)
    {
        try
        {
            await this._next(context);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Unhandled exception occurred. Request ID: {TraceId}", context.TraceIdentifier);

            // Check if user is admin
            var isAdmin = false;
            if (currentUserService.TryGetUserId(context.User, out var userId))
            {
                try
                {
                    var user = await userRepository.FindByIdAsync(userId);
                    isAdmin = user?.SystemAdmin ?? false;
                }
                catch (Exception uex)
                {
                    this._logger.LogError(uex, "Error retrieving user info for exception handling");
                }
            }

            // Store exception details in HttpContext for the error page
            context.Items["Exception"] = ex;
            context.Items["IsAdmin"] = isAdmin;
            context.Items["TraceId"] = context.TraceIdentifier;

            if (context.Response.HasStarted)
            {
                this._logger.LogWarning("Response has already started, cannot redirect to error page");
                throw;
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/html";

            // Redirect to error page
            context.Request.Path = "/Error";
            context.Request.QueryString = QueryString.Empty;

            await this._next(context);
        }
    }
}

/// <summary>
/// Extension method for adding the global exception handler middleware to the pipeline.
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) => app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}
