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

        var email = Input.Email?.Trim() ?? string.Empty;
        // Ensure password is never null, use empty string
        var password = Input.Password ?? string.Empty;
        var result = await _authService.AuthenticateAsync(email, password);
        
        // Check if user needs to set password first
        if (result.RequiresPasswordSetup && result.UserId.HasValue)
        {
            return Redirect($"/setpassword?userId={result.UserId}");
        }
        
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        Response.Cookies.Append("user_token", result.Token!, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }

        if (!string.IsNullOrEmpty(result.WorkspaceSlug))
        {
            return Redirect($"/workspaces/{result.WorkspaceSlug}");
        }

        // Fallback: user doesn't have a workspace assigned
        ErrorMessage = "No workspace found for your account. Please contact support.";
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



