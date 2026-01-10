using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;

namespace Tickflo.Web.Pages.Users;

[Authorize]
public class EditModel : PageModel
{
    private readonly IUserRepository _users;

    public EditModel(IUserRepository users)
    {
        _users = users;
    }

    [BindProperty]
    public InputModel? Input { get; set; }

    public int UserId { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(300)]
        public string Email { get; set; } = string.Empty;

        [EmailAddress]
        [Display(Name = "Recovery Email")]
        public string? RecoveryEmail { get; set; }

        [Display(Name = "System Admin")]
        public bool SystemAdmin { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        var me = await _users.FindByIdAsync(userId);
        if (me?.SystemAdmin != true)
            return Forbid();

        var user = await _users.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        UserId = id;
        Input = new InputModel
        {
            Name = user.Name,
            Email = user.Email,
            RecoveryEmail = user.RecoveryEmail,
            SystemAdmin = user.SystemAdmin
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!TryGetUserId(out var userId))
            return Forbid();
        var me = await _users.FindByIdAsync(userId);
        if (me?.SystemAdmin != true)
            return Forbid();

        var user = await _users.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            UserId = id;
            return Page();
        }

        // Ensure unique email if changed
        if (!string.Equals(user.Email, Input!.Email, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _users.FindByEmailAsync(Input.Email);
            if (exists != null && exists.Id != user.Id)
            {
                ModelState.AddModelError(nameof(Input.Email), "A user with this email already exists.");
                UserId = id;
                return Page();
            }
        }

        user.Name = Input.Name.Trim();
        user.Email = Input.Email.Trim().ToLowerInvariant();
        user.RecoveryEmail = string.IsNullOrWhiteSpace(Input.RecoveryEmail) ? null : Input.RecoveryEmail!.Trim().ToLowerInvariant();
        user.SystemAdmin = Input.SystemAdmin;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.UpdateAsync(user);

        TempData["Message"] = "User updated.";
        return RedirectToPage("/Users/Index");
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
