using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;

namespace Tickflo.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExceptionDetails { get; set; }
    public new int? StatusCode { get; set; }
    public bool IsAdmin { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public ErrorModel(ILogger<ErrorModel> logger, ICurrentUserService currentUserService, IUserRepository userRepository)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    public async Task OnGetAsync()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        // Check if user is a system admin
        if (_currentUserService.TryGetUserId(User, out var userId))
        {
            var user = await _userRepository.FindByIdAsync(userId);
            IsAdmin = user?.SystemAdmin ?? false;
        }

        // Extract error details from HttpContext
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        
        if (exceptionHandlerPathFeature?.Error is Exception ex)
        {
            ErrorMessage = ex.Message;
            if (IsAdmin)
            {
                ExceptionDetails = BuildExceptionDetails(ex);
            }
            _logger.LogError(ex, "Unhandled exception occurred. Request ID: {RequestId}", RequestId);
        }

        // Try to get status code
        StatusCode = HttpContext.Response.StatusCode;
    }

    private string BuildExceptionDetails(Exception ex)
    {
        var details = new System.Text.StringBuilder();
        details.AppendLine($"<strong>Exception Type:</strong> {ex.GetType().FullName}");
        details.AppendLine($"<strong>Message:</strong> {ex.Message}");
        details.AppendLine($"<strong>Stack Trace:</strong>");
        details.AppendLine($"<pre class='text-xs'>{System.Web.HttpUtility.HtmlEncode(ex.StackTrace)}</pre>");
        
        if (ex.InnerException != null)
        {
            details.AppendLine($"<strong>Inner Exception:</strong>");
            details.AppendLine($"<pre class='text-xs'>{System.Web.HttpUtility.HtmlEncode(ex.InnerException.Message)}</pre>");
        }
        
        return details.ToString();
    }
}

