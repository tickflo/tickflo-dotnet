namespace Tickflo.Web.Pages.Account;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Authentication;

public class SetPasswordModel(IPasswordSetupService passwordSetupService) : PageModel
{
    private readonly IPasswordSetupService passwordSetupService = passwordSetupService;

    [BindProperty(SupportsGet = true)]
    public string Token { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [StringLength(200, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? Error { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var validation = await this.passwordSetupService.ValidateResetTokenAsync(this.Token);
        if (!validation.IsValid)
        {
            this.Error = validation.ErrorMessage;
        }
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.ModelState.IsValid)
        {
            var validation = await this.passwordSetupService.ValidateResetTokenAsync(this.Token);
            if (!validation.IsValid)
            {
                this.Error = validation.ErrorMessage;
            }
            return this.Page();
        }

        var result = await this.passwordSetupService.SetPasswordWithTokenAsync(this.Token, this.Password);
        if (!result.Success)
        {
            this.Error = result.ErrorMessage;
            return this.Page();
        }

        this.TempData["Message"] = "Password updated. You can now sign in.";
        return this.Redirect("/login");
    }
}

