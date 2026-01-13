using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Common;
namespace Tickflo.Web.Pages.Users;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    public CreateModel(IUserRepository users, IPasswordHasher passwordHasher, ICurrentUserService currentUserService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

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

        [Required]
        [StringLength(200, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        if (!_currentUserService.TryGetUserId(User, out var userId))
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
        var meTask = _users.FindByIdAsync(userId);
        meTask.Wait();
        var me = meTask.Result;
        if (me?.SystemAdmin != true)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_currentUserService.TryGetUserId(User, out var userId))
            return Forbid();
        var me = await _users.FindByIdAsync(userId);
        if (me?.SystemAdmin != true)
            return Forbid();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existing = await _users.FindByEmailAsync(Input.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(Input.Email), "A user with this email already exists.");
            return Page();
        }

        var user = new User
        {
            Name = Input.Name.Trim(),
            Email = Input.Email.Trim().ToLowerInvariant(),
            RecoveryEmail = string.IsNullOrWhiteSpace(Input.RecoveryEmail) ? null : Input.RecoveryEmail!.Trim().ToLowerInvariant(),
            SystemAdmin = Input.SystemAdmin,
            EmailConfirmed = false,
            PasswordHash = _passwordHasher.Hash(Input.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user);

        TempData["Message"] = "User created.";
        return RedirectToPage("/Workspace");
    }
}


