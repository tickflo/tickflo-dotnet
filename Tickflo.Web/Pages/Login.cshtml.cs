using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class LoginModel(ILogger<LoginModel> logger, IAuthenticationService authService) : PageModel
{
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
        {
            return Page();
        }

        var result = await _authService.AuthenticateAsync(Input.Email, Input.Password);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        Response.Cookies.Append("user_token", result.Token!, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        if (ReturnUrl != null)
        {
            return Redirect(ReturnUrl);
        }

        if (!string.IsNullOrEmpty(result.WorkspaceSlug))
        {
            return Redirect($"/{result.WorkspaceSlug}");
        }

        return Redirect("/workspaces");
    }
}

public class LoginInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

