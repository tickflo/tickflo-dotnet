namespace Tickflo.Web.Pages.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Admin;

[Authorize]
public partial class EmailLogModel(IEmailLogService emailLogService) : PageModel
{
    private readonly IEmailLogService emailLogService = emailLogService;
    public List<EmailLogEntry>? Emails { get; set; }
    public int TotalEmails { get; set; }
    public async Task<IActionResult> OnGetAsync([FromServices] IAppContext appContext)
    {
        var user = appContext.CurrentUser;
        if (user == null || !user.SystemAdmin)
        {
            return this.Forbid();
        }

        var (total, emails) = await this.emailLogService.GetEmailsAsync();
        this.TotalEmails = total;
        this.Emails = emails;

        if (this.Emails == null)
        {
            return this.Page();
        }

        var linkRegex = UriRegex();
        foreach (var email in this.Emails)
        {
            email.Body = linkRegex.Replace(email.Body, "<a style=\"color:#2300EE;\" href=\"$1\" target=\"_blank\">$1</a>").Replace("\n", "<br/>");
        }

        return this.Page();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"(https?:\/\/[^\s]+)")]
    private static partial System.Text.RegularExpressions.Regex UriRegex();
}
