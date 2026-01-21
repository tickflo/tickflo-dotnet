using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Authentication;

namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class LoginModel(ILogger<LoginModel> logger, IAuthenticationService authService) : PageModel
{
    #region Constants
    private const string TokenCookieName = "user_token";
    private const int TokenCookieExpirationDays = 30;
    private const string PasswordSetupUrl = "/setpassword?userId={0}";
    private const string WorkspaceRedirectUrl = "/workspaces/{0}";
    private const string WorkspacesUrl = "/workspaces";
    private const string NoWorkspaceError = "No workspace found for your account. Please contact support.";
    private const string PasswordSetupRequiredMessage = "Please set your password before logging in.";
    #endregion

    private readonly ILogger<LoginModel> _logger = logger;
    private readonly IAuthenticationService _authService = authService;

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var email = Input.Email?.Trim() ?? string.Empty;
        var password = Input.Password ?? string.Empty;
        var result = await _authService.AuthenticateAsync(email, password);

        if (result.RequiresPasswordSetup && result.UserId.HasValue)
            return Redirect(string.Format(PasswordSetupUrl, result.UserId));

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        AppendAuthenticationCookie(result.Token!);
        return GetPostLoginRedirect(result.WorkspaceSlug);
    }

    private void AppendAuthenticationCookie(string token)
    {
        Response.Cookies.Append(TokenCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(TokenCookieExpirationDays)
        });
    }

    private IActionResult GetPostLoginRedirect(string? workspaceSlug)
    {
        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return Redirect(ReturnUrl);

        if (!string.IsNullOrEmpty(workspaceSlug))
            return Redirect(string.Format(WorkspaceRedirectUrl, workspaceSlug));

        ErrorMessage = NoWorkspaceError;
        return Page();
    }
}

public class LoginInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}



