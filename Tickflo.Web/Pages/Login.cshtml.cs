namespace Tickflo.Web.Pages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;
using Tickflo.Core.Services.Authentication;

[AllowAnonymous]
public class LoginModel(IAuthenticationService authenticationService, TickfloConfig config) : PageModel
{
    private readonly IAuthenticationService authenticationService = authenticationService;
    private readonly TickfloConfig config = config;

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

        this.AppendAuthenticationCookie(result.Token);
        return this.Redirect("/workspaces");
    }

    private void AppendAuthenticationCookie(string token) => this.Response.Cookies.Append(this.config.SessionCookieName, token, new CookieOptions
    {
        HttpOnly = true,
        Secure = this.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Expires = DateTimeOffset.UtcNow.AddMinutes(this.config.SessionTimeoutMinutes)
    });
}

public class LoginInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";
}



