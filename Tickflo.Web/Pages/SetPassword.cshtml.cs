using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class SetPasswordModel(
    ILogger<SetPasswordModel> logger,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenRepository tokenRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IWorkspaceRepository workspaceRepository) : PageModel
{
    private readonly ILogger<SetPasswordModel> _logger = logger;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenRepository _tokenRepository = tokenRepository;
    private readonly IUserWorkspaceRepository _userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceRepository _workspaceRepository = workspaceRepository;

    [BindProperty]
    public SetPasswordInput Input { get; set; } = new();

    [FromQuery]
    public int UserId { get; set; }

    public string? ErrorMessage { get; set; }
    public string? UserEmail { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId <= 0)
        {
            return Redirect("/login");
        }

        var user = await _userRepository.FindByIdAsync(UserId);
        if (user == null)
        {
            return Redirect("/login");
        }

        // Only allow if they don't have a password yet
        if (user.PasswordHash != null)
        {
            return Redirect("/login");
        }

        UserEmail = user.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UserId <= 0)
        {
            return Redirect("/login");
        }

        var user = await _userRepository.FindByIdAsync(UserId);
        if (user == null || user.PasswordHash != null)
        {
            return Redirect("/login");
        }

        if (!ModelState.IsValid)
        {
            UserEmail = user.Email;
            return Page();
        }

        if (Input.Password != Input.ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            UserEmail = user.Email;
            return Page();
        }

        if (Input.Password.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters long";
            UserEmail = user.Email;
            return Page();
        }

        // Hash password using email + password
        var passwordHash = _passwordHasher.Hash($"{user.Email}{Input.Password}");
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        // Automatically log the user in
        var token = await _tokenRepository.CreateForUserIdAsync(user.Id);
        Response.Cookies.Append("user_token", token.Value, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        // Find user's first accepted workspace and redirect
        var uw = await _userWorkspaceRepository.FindAcceptedForUserAsync(user.Id);
        if (uw != null)
        {
            var ws = await _workspaceRepository.FindByIdAsync(uw.WorkspaceId);
            if (ws != null)
            {
                return Redirect($"/workspaces/{ws.Slug}");
            }
        }

        // Fallback to home if no workspace found
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
