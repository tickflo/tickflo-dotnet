using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Web.Pages.Account;

public class SetPasswordModel : PageModel
{
    private readonly ITokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public SetPasswordModel(ITokenRepository tokens, IUserRepository users, IPasswordHasher hasher)
    {
        _tokens = tokens;
        _users = users;
        _hasher = hasher;
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
        if (string.IsNullOrWhiteSpace(Token))
        {
            Error = "Missing token.";
            return Page();
        }
        var tok = await _tokens.FindByValueAsync(Token);
        if (tok == null)
        {
            Error = "Invalid or expired token.";
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tok = await _tokens.FindByValueAsync(Token);
        if (tok == null)
        {
            Error = "Invalid or expired token.";
            return Page();
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _users.FindByIdAsync(tok.UserId);
        if (user == null)
        {
            Error = "User not found.";
            return Page();
        }

        user.PasswordHash = _hasher.Hash(Password);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        TempData["Message"] = "Password updated. You can now sign in.";
        return Redirect("/login");
    }
}
