using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services;


namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class SignupModel(ILogger<SignupModel> logger, IAuthenticationService authService) : PageModel
{
    private readonly ILogger<SignupModel> _logger = logger;
    private readonly IAuthenticationService _authService = authService;

    [BindProperty]
    public SignupInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        // Custom validation: Recovery email must be different from login email
        if (!string.IsNullOrEmpty(Input.Email) && !string.IsNullOrEmpty(Input.RecoveryEmail) &&
            Input.Email.Equals(Input.RecoveryEmail, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.RecoveryEmail", "Recovery email must be different from your login email.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }
        var result = await _authService.SignupAsync(Input.Name, Input.Email, Input.RecoveryEmail, Input.WorkspaceName, Input.Password);
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

        if (!string.IsNullOrEmpty(result.WorkspaceSlug))
        {
            return Redirect($"/workspaces/{result.WorkspaceSlug}");
        }

        return Redirect("/workspaces");
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
