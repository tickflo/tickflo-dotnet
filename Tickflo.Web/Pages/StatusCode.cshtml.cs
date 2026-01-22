namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class StatusCodeModel(ILogger<StatusCodeModel> logger) : PageModel
{
    public int HttpStatusCode { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public string? TraceId { get; set; }

    private readonly ILogger<StatusCodeModel> logger = logger;

    public async Task OnGetAsync(int code)
    {
        this.HttpStatusCode = code;
        this.TraceId = this.HttpContext.TraceIdentifier;

        (this.StatusMessage, this.StatusDescription) = code switch
        {
            400 => ("Bad Request", "The request was invalid or malformed. Please check your input and try again."),
            401 => ("Unauthorized", "You need to be logged in to access this page. Please log in and try again."),
            403 => ("Forbidden", "You don't have permission to access this resource."),
            404 => ("Page Not Found", "The page you're looking for doesn't exist or has been moved."),
            408 => ("Request Timeout", "Your request took too long to process. Please try again."),
            429 => ("Too Many Requests", "You're making too many requests. Please wait a moment and try again."),
            500 => ("Internal Server Error", "Something went wrong on our end. Our team has been notified."),
            502 => ("Bad Gateway", "The server received an invalid response. Please try again later."),
            503 => ("Service Unavailable", "The service is temporarily unavailable. Please try again later."),
            504 => ("Gateway Timeout", "The server didn't respond in time. Please try again later."),
            _ => ("Error", "An unexpected error occurred. Please try again.")
        };

        this.logger.LogWarning("Status code {StatusCode} - {StatusMessage}. Trace ID: {TraceId}", this.HttpStatusCode, this.StatusMessage, this.TraceId);
    }
}
