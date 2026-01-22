namespace Tickflo.Web.Pages;

using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Authentication;

[AllowAnonymous]
public class LoginModel(IAuthenticationService authenticationService) : PageModel
{
    #region Constants
    private const string TokenCookieName = "user_token";
    private const int TokenCookieExpirationDays = 30;
    private static readonly CompositeFormat PasswordSetupUrl = CompositeFormat.Parse("/setpassword?userId={0}");
    private static readonly CompositeFormat WorkspaceRedirectUrl = CompositeFormat.Parse("/workspaces/{0}");
    private const string NoWorkspaceError = "No workspace found for your account. Please contact support.";
    #endregion

    private readonly IAuthenticationService authenticationService = authenticationService;

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        var email = this.Input.Email?.Trim() ?? string.Empty;
        var password = this.Input.Password ?? string.Empty;
        var result = await this.authenticationService.AuthenticateAsync(email, password);

        if (result.RequiresPasswordSetup && result.UserId.HasValue)
        {
            return this.Redirect(string.Format(null, PasswordSetupUrl, result.UserId));
        }

        if (!result.Success)
        {
            this.ErrorMessage = result.ErrorMessage;
            return this.Page();
        }

        this.AppendAuthenticationCookie(result.Token!);
        return this.GetPostLoginRedirect(result.WorkspaceSlug);
    }

    private void AppendAuthenticationCookie(string token) => this.Response.Cookies.Append(TokenCookieName, token, new CookieOptions
    {
        HttpOnly = true,
        Secure = this.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddDays(TokenCookieExpirationDays)
    });

    private IActionResult GetPostLoginRedirect(string? workspaceSlug)
    {
        if (!string.IsNullOrWhiteSpace(this.ReturnUrl) && this.Url.IsLocalUrl(this.ReturnUrl))
        {
            return this.Redirect(this.ReturnUrl);
        }

        if (!string.IsNullOrEmpty(workspaceSlug))
        {
            return this.Redirect(string.Format(null, WorkspaceRedirectUrl, workspaceSlug));
        }

        this.ErrorMessage = NoWorkspaceError;
        return this.Page();
    }
}

public class LoginInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}



