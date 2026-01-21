namespace Tickflo.Web.Pages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Authentication;

[AllowAnonymous]
public class SignupModel(ILogger<SignupModel> logger, IAuthenticationService authService) : PageModel
{
    #region Constants
    private const string TokenCookieName = "user_token";
    private const int TokenCookieExpirationDays = 30;
    private const string WorkspaceRedirectUrl = "/workspaces/{0}";
    private const string WorkspacesUrl = "/workspaces";
    private const string RecoveryEmailMismatchError = "Recovery email must be different from your login email.";
    private const string RecoveryEmailFieldName = "Input.RecoveryEmail";
    #endregion

    private readonly ILogger<SignupModel> logger = logger;
    private readonly IAuthenticationService _authService = authService;

    [BindProperty]
    public SignupInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (this.ValidateRecoveryEmailDifference() is IActionResult validationResult)
        {
            return validationResult;
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var result = await this.ExecuteSignupAsync();
        if (!result.Success)
        {
            this.ErrorMessage = result.ErrorMessage;
            return this.Page();
        }

        this.AppendAuthenticationCookie(result.Token!);
        return this.GetPostSignupRedirect(result.WorkspaceSlug);
    }

    private IActionResult? ValidateRecoveryEmailDifference()
    {
        if (!string.IsNullOrEmpty(this.Input.Email) && !string.IsNullOrEmpty(this.Input.RecoveryEmail) &&
            this.Input.Email.Equals(this.Input.RecoveryEmail, StringComparison.OrdinalIgnoreCase))
        {
            this.ModelState.AddModelError(RecoveryEmailFieldName, RecoveryEmailMismatchError);
        }

        return null;
    }

    private async Task<AuthenticationResult> ExecuteSignupAsync()
    {
        var name = this.Input.Name?.Trim() ?? string.Empty;
        var email = this.Input.Email?.Trim() ?? string.Empty;
        var recoveryEmail = this.Input.RecoveryEmail?.Trim() ?? string.Empty;
        var workspaceName = this.Input.WorkspaceName?.Trim() ?? string.Empty;
        var password = this.Input.Password ?? string.Empty;

        return await this._authService.SignupAsync(name, email, recoveryEmail, workspaceName, password);
    }

    private void AppendAuthenticationCookie(string token) => this.Response.Cookies.Append(TokenCookieName, token, new CookieOptions
    {
        HttpOnly = true,
        Secure = this.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(TokenCookieExpirationDays)
    });

    private IActionResult GetPostSignupRedirect(string? workspaceSlug)
    {
        if (!string.IsNullOrEmpty(workspaceSlug))
        {
            return this.Redirect(string.Format(WorkspaceRedirectUrl, workspaceSlug));
        }

        return this.Redirect(WorkspacesUrl);
    }
}

public class SignupInput
{
    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Recovery Email")]
    public string RecoveryEmail { get; set; } = "";

    [Required]
    [Display(Name = "Workspace Name")]
    [StringLength(50, MinimumLength = 1)]
    public string WorkspaceName { get; set; } = "";

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}


