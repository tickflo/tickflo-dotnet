namespace Tickflo.Web.Pages;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Authentication;

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
        var validation = await this._passwordSetupService.ValidateInitialUserAsync(this.UserId);
        if (!validation.IsValid)
        {
            return this.Redirect("/login");
        }

        this.UserEmail = validation.UserEmail;
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var validation = await this._passwordSetupService.ValidateInitialUserAsync(this.UserId);
        if (!validation.IsValid || validation.UserId == null)
        {
            return this.Redirect("/login");
        }

        if (!this.ModelState.IsValid)
        {
            this.UserEmail = validation.UserEmail;
            return this.Page();
        }

        var result = await this._passwordSetupService.SetInitialPasswordAsync(validation.UserId.Value, this.Input.Password);
        if (!result.Success || string.IsNullOrWhiteSpace(result.LoginToken))
        {
            this.ErrorMessage = result.ErrorMessage ?? "Could not set password.";
            this.UserEmail = validation.UserEmail;
            return this.Page();
        }
        this.Response.Cookies.Append("user_token", result.LoginToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = this.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        if (!string.IsNullOrWhiteSpace(result.WorkspaceSlug))
        {
            return this.Redirect($"/workspaces/{result.WorkspaceSlug}");
        }

        return this.Redirect("/");
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

