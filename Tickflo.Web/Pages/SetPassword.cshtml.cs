using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class SetPasswordModel(
    ILogger<SetPasswordModel> logger,
    IPasswordSetupService passwordSetupService) : PageModel
{
    private readonly ILogger<SetPasswordModel> _logger = logger;
    private readonly IPasswordSetupService _passwordSetupService = passwordSetupService;

    [BindProperty]
    public SetPasswordInput Input { get; set; } = new();

    [FromQuery]
    public int UserId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? UserEmail { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var validation = await _passwordSetupService.ValidateInitialUserAsync(UserId);
        if (!validation.IsValid)
        {
            return Redirect("/login");
        }

        UserEmail = validation.UserEmail;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validation = await _passwordSetupService.ValidateInitialUserAsync(UserId);
        if (!validation.IsValid || validation.UserId == null)
        {
            return Redirect("/login");
        }

        if (!ModelState.IsValid)
        {
            UserEmail = validation.UserEmail;
            return Page();
        }

        var result = await _passwordSetupService.SetInitialPasswordAsync(validation.UserId.Value, Input.Password);
        if (!result.Success || string.IsNullOrWhiteSpace(result.LoginToken))
        {
            ErrorMessage = result.ErrorMessage ?? "Could not set password.";
            UserEmail = validation.UserEmail;
            return Page();
        }
        Response.Cookies.Append("user_token", result.LoginToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        if (!string.IsNullOrWhiteSpace(result.WorkspaceSlug))
        {
            return Redirect($"/workspaces/{result.WorkspaceSlug}");
        }

        return Redirect("/");
    }
}

public class SetPasswordInput
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}
