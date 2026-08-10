namespace Tickflo.Web.Middleware;

using System.Collections.Concurrent;

/// <summary>
/// Simple in-memory rate limiting middleware for auth endpoints.
/// Tracks requests per IP per endpoint with a fixed window.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate next;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> Buckets = new();
    private const int MaxRequests = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    // Auth-related paths that should be rate-limited
    private static readonly string[] AuthPaths =
    [
        "/login",
        "/signup",
        "/forgot-password",
        "/reset-password",
        "/set-password",
        "/api/send-emails",
        "/email-confirmation/confirm",
        "/email-confirmation/resend"
    ];

    public RateLimitMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();

        if (path != null && PathRequiresRateLimiting(path))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"{ip}:{path}";
            var now = DateTime.UtcNow;

            var entry = Buckets.GetOrAdd(key, _ => new RateLimitEntry { WindowStart = now, Count = 0 });
            var blocked = false;

            lock (entry)
            {
                if (now - entry.WindowStart > Window)
                {
                    entry.WindowStart = now;
                    entry.Count = 0;
                }

                if (entry.Count >= MaxRequests)
                {
                    blocked = true;
                }
                else
                {
                    entry.Count++;
                }
            }

            if (blocked)
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." });
                return;
            }
        }

        await this.next(context);
    }

    private static bool PathRequiresRateLimiting(string path)
    {
        foreach (var authPath in AuthPaths)
        {
            if (path.StartsWith(authPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private class RateLimitEntry
    {
        public DateTime WindowStart { get; set; }
        public int Count { get; set; }
    }
}
