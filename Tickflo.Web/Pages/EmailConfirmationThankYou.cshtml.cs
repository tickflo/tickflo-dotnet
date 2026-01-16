using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Tickflo.Web.Pages;

public class EmailConfirmationThankYouModel : PageModel
{
    private readonly IEmailTemplateRepository _emailTemplateRepo;

    public EmailConfirmationThankYouModel(IEmailTemplateRepository emailTemplateRepo)
    {
        _emailTemplateRepo = emailTemplateRepo;
    }

    public string TemplateContent { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var template = await _emailTemplateRepo.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou);
        
        if (template == null)
        {
            // Fallback to default content if template not found
            TemplateContent = GetDefaultContent();
        }
        else
        {
            TemplateContent = template.Body;
            
            // Replace navigation buttons placeholder based on authentication status
            var navigationButtons = User?.Identity?.IsAuthenticated ?? false
                ? "<a class=\"btn btn-primary\" href=\"/workspaces\">Go to Workspace</a>"
                : "<a class=\"btn btn-primary\" href=\"/login\">Go to Login</a>";
            navigationButtons += "<a class=\"btn btn-ghost\" href=\"/\">Back to Home</a>";
            
            TemplateContent = TemplateContent.Replace("{{NAVIGATION_BUTTONS}}", navigationButtons);
        }

        return Page();
    }

    private string GetDefaultContent()
    {
        var navigationButtons = User?.Identity?.IsAuthenticated ?? false
            ? "<a class=\"btn btn-primary\" href=\"/workspaces\">Go to Workspace</a>"
            : "<a class=\"btn btn-primary\" href=\"/login\">Go to Login</a>";
        navigationButtons += "<a class=\"btn btn-ghost\" href=\"/\">Back to Home</a>";

        return $@"<div class=""min-h-[60vh] flex items-center justify-center"">
    <div class=""card w-full max-w-lg bg-base-100 shadow-xl border border-base-200/60 rounded-3xl"">
        <div class=""card-body space-y-4 text-center"">
            <div class=""inline-flex items-center justify-center w-14 h-14 rounded-full bg-success/10 text-success"">
                <i class=""fa fa-check text-2xl""></i>
            </div>
            <h1 class=""text-2xl font-bold"">Thank you for confirming</h1>
            <p class=""text-base-content/70"">Your email address has been confirmed. You can now continue to sign in and use your workspace.</p>
            <div class=""pt-2 flex items-center justify-center gap-3"">
                {navigationButtons}
            </div>
        </div>
    </div>
</div>";
    }
}
