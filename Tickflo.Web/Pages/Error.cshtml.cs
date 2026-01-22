namespace Tickflo.Web.Pages;

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel(ILogger<ErrorModel> logger, ICurrentUserService currentUserService, IUserRepository userRepository) : PageModel
{
    public string? RequestId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExceptionDetails { get; set; }
    public new int? StatusCode { get; set; }
    public bool IsAdmin { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

    private readonly ILogger<ErrorModel> logger = logger;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private readonly IUserRepository userRepository = userRepository;

    public async Task OnGetAsync()
    {
        this.RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier;

        // Check if user is a system admin
        if (this.currentUserService.TryGetUserId(this.User, out var userId))
        {
            var user = await this.userRepository.FindByIdAsync(userId);
            this.IsAdmin = user?.SystemAdmin ?? false;
        }

        // Extract error details from HttpContext
        var exceptionHandlerPathFeature = this.HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature?.Error is Exception ex)
        {
            this.ErrorMessage = ex.Message;
            if (this.IsAdmin)
            {
                this.ExceptionDetails = BuildExceptionDetails(ex);
            }
            this.logger.LogError(ex, "Unhandled exception occurred. Request ID: {RequestId}", this.RequestId);
        }

        // Try to get status code
        this.StatusCode = this.HttpContext.Response.StatusCode;
    }

    private static string BuildExceptionDetails(Exception ex)
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

