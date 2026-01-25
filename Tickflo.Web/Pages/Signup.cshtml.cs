namespace Tickflo.Web.Pages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Exceptions;
using Tickflo.Core.Services.Authentication;

[AllowAnonymous]
public class SignupModel(IAuthenticationService authenticationService, TickfloConfig config, ILogger<SignupModel> logger) : PageModel
{
    private readonly IAuthenticationService authenticationService = authenticationService;
    private readonly TickfloConfig config = config;
    private readonly ILogger<SignupModel> logger = logger;

    [Required]
    [Display(Name = "Name")]
    [BindProperty]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    [BindProperty]
    public string Email { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Recovery Email")]
    [BindProperty]
    public string RecoveryEmail { get; set; } = "";

    [Display(Name = "Workspace Name")]
    [BindProperty]
    public string? WorkspaceName { get; set; } = null;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [BindProperty]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [BindProperty]
    public string ConfirmPassword { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public bool Invited { get; set; }

    public async Task OnGetAsync([FromQuery] string? email)
    {
        if (!string.IsNullOrEmpty(email))
        {
            this.Email = email;
            this.Invited = true;
        }
    }

    public async Task<IActionResult> OnPostAsync([FromQuery] string? email)
    {
        this.Invited = !string.IsNullOrEmpty(email);
        this.ValidateRecoveryEmailDifference();
        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        try
        {
            var result = await this.ExecuteSignupAsync();
            this.AppendAuthenticationCookie(result.Token);
            return this.Redirect("/workspaces");
        }
        catch (HttpException ex)
        {
            this.ErrorMessage = ex.Message;
            this.logger.LogError(ex, "Error during signup for email {Email}", this.Email);
            return this.Page();
        }
    }

    private void ValidateRecoveryEmailDifference()
    {
        if (!string.IsNullOrEmpty(this.Email) && !string.IsNullOrEmpty(this.RecoveryEmail) &&
            this.Email.Equals(this.RecoveryEmail, StringComparison.OrdinalIgnoreCase))
        {
            this.ModelState.AddModelError("RecoveryEmail", "Recovery email must be different from the primary email.");
        }
    }

    private async Task<AuthenticationResult> ExecuteSignupAsync()
    {
        var name = this.Name?.Trim() ?? string.Empty;
        var email = this.Email?.Trim() ?? string.Empty;
        var recoveryEmail = this.RecoveryEmail?.Trim() ?? string.Empty;
        var workspaceName = this.WorkspaceName?.Trim() ?? string.Empty;
        var password = this.Password ?? string.Empty;
        return workspaceName.Length > 0
            ? await this.authenticationService.SignupAsync(name, email, recoveryEmail, workspaceName, password)
            : await this.authenticationService.SignupInviteeAsync(name, email, recoveryEmail, password);
    }

    private void AppendAuthenticationCookie(string token) => this.Response.Cookies.Append(this.config.SessionCookieName, token, new CookieOptions
    {
        HttpOnly = true,
        Secure = this.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddMinutes(this.config.SessionTimeoutMinutes),
    });
}
