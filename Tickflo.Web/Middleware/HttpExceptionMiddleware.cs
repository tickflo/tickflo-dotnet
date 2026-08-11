namespace Tickflo.Web.Middleware;

using Tickflo.Core.Exceptions;

/// <summary>
/// Middleware that catches HttpException and maps it to the appropriate HTTP status code.
/// Prevents InvalidOperationException from bubbling up as a generic 500.
/// </summary>
public class HttpExceptionMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await this.next(context);
        }
        catch (HttpException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
    }
}
