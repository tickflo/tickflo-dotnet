using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Web.Pages.Account;

public class SetPasswordModel : PageModel
{
    private readonly IPasswordSetupService _passwordSetupService;

    public SetPasswordModel(IPasswordSetupService passwordSetupService)
    {
        _passwordSetupService = passwordSetupService;
    }

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
        var validation = await _passwordSetupService.ValidateResetTokenAsync(Token);
        if (!validation.IsValid)
        {
            Error = validation.ErrorMessage;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var validation = await _passwordSetupService.ValidateResetTokenAsync(Token);
            if (!validation.IsValid)
            {
                Error = validation.ErrorMessage;
            }
            return Page();
        }

        var result = await _passwordSetupService.SetPasswordWithTokenAsync(Token, Password);
        if (!result.Success)
        {
            Error = result.ErrorMessage;
            return Page();
        }

        TempData["Message"] = "Password updated. You can now sign in.";
        return Redirect("/login");
    }
}
