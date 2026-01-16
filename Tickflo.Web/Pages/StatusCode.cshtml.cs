using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;

namespace Tickflo.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class StatusCodeModel : PageModel
{
    public int HttpStatusCode { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public string? TraceId { get; set; }

    private readonly ILogger<StatusCodeModel> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public StatusCodeModel(ILogger<StatusCodeModel> logger, ICurrentUserService currentUserService, IUserRepository userRepository)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task OnGetAsync(int code)
    {
        HttpStatusCode = code;
        TraceId = HttpContext.TraceIdentifier;

        (StatusMessage, StatusDescription) = code switch
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

        _logger.LogWarning("Status code {StatusCode} - {StatusMessage}. Trace ID: {TraceId}", HttpStatusCode, StatusMessage, TraceId);
    }
}
