using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
// using Tickflo.Core.Data; // removed duplicate using
using Tickflo.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Tickflo.Core.Services.Auth;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;
using System.Security.Cryptography;
using Tickflo.Core.Data;
using Microsoft.AspNetCore.Authorization;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class UsersInviteModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly ITokenRepository _tokenRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [BindProperty]
    public string Role { get; set; } = "Member";

    public UsersInviteModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IPasswordHasher passwordHasher, IEmailSender emailSender, ITokenRepository tokenRepo, IRoleRepository roleRepo, IRolePermissionRepository rolePerms)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _tokenRepo = tokenRepo;
        _roleRepo = roleRepo;
        _rolePerms = rolePerms;
    }
    public bool CanViewUsers { get; private set; }
    public bool CanCreateUsers { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var access = await EnsureAccessAsync(slug);
        if (access.failure != null) return access.failure;

        Workspace = access.workspace;
        CanViewUsers = access.canView;
        CanCreateUsers = access.canCreate;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        var access = await EnsureAccessAsync(slug);
        if (access.failure != null) return access.failure;

        Workspace = access.workspace;
        var currentUserId = access.userId;
        CanViewUsers = access.canView;
        CanCreateUsers = access.canCreate;

        var ws = Workspace;
        if (ws == null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var emailNorm = Email.Trim().ToLowerInvariant();
        var user = await _userRepo.FindByEmailAsync(emailNorm);
        if (user == null)
        {
            // Generate strong temp password
            var tempPassword = GenerateStrongPassword();
            var confirmCode = TokenGenerator.GenerateToken(16);

            user = new User
            {
                Name = emailNorm, // default to email; could be improved later
                Email = emailNorm,
                EmailConfirmed = false,
                EmailConfirmationCode = confirmCode,
                PasswordHash = _passwordHasher.Hash(tempPassword),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            };
            await _userRepo.AddAsync(user);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var confirmationLink = $"{baseUrl}/email-confirmation/confirm?email={Uri.EscapeDataString(emailNorm)}&code={Uri.EscapeDataString(confirmCode)}";
            var acceptToken = await _tokenRepo.CreateForUserIdAsync(user.Id);
            var resetToken = await _tokenRepo.CreatePasswordResetForUserIdAsync(user.Id);
            var acceptLink = $"{baseUrl}/workspaces/{Uri.EscapeDataString(ws.Slug)}/accept?token={Uri.EscapeDataString(acceptToken.Value)}";
            var setPasswordLink = $"{baseUrl}/account/set-password?token={Uri.EscapeDataString(resetToken.Value)}";
            var subject = $"You're invited to {ws.Name}";
            var body = $"<div style='font-family:Arial,sans-serif'>"+
                        $"<h2 style='color:#333'>Workspace Invitation</h2>"+
                        $"<p>You have been invited to the workspace '<b>{ws.Name}</b>'.</p>"+
                        $"<p>Temporary password: <code style='font-size:1.1em'>{tempPassword}</code></p>"+
                        $"<p>Please confirm your email: <a href=\"{confirmationLink}\">Confirm Email</a></p>"+
                        $"<p>Then accept the invite: <a href=\"{acceptLink}\">Accept Invite</a></p>"+
                        $"<p>Or set your password now: <a href=\"{setPasswordLink}\">Set Password</a></p>"+
                        $"<hr/><p style='color:#777'>If you did not expect this email, you can ignore it.</p>"+
                        $"</div>";
            await _emailSender.SendAsync(emailNorm, subject, body);
        }

        if (user == null)
        {
            throw new InvalidOperationException("User creation failed.");
        }

        var invite = new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = ws.Id,
            Accepted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId
        };
        await _userWorkspaceRepo.AddAsync(invite);

        // Assign selected role to the invited user if role exists in this workspace
        var selectedRoleName = Role?.Trim();
        if (!string.IsNullOrWhiteSpace(selectedRoleName))
        {
            var role = await _roleRepo.FindByNameAsync(ws.Id, selectedRoleName);
            if (role == null)
            {
                var adminFlag = string.Equals(selectedRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
                role = await _roleRepo.AddAsync(ws.Id, selectedRoleName, adminFlag, currentUserId);
            }
            await _userWorkspaceRoleRepo.AddAsync(user.Id, ws.Id, role.Id, currentUserId);
        }

        // role assignment handled above

        TempData["Success"] = $"Invite created for '{Email}'" + (!string.IsNullOrWhiteSpace(Role) ? $" as {Role}" : "") + ".";
        return RedirectToPage("/Workspaces/Users", new { slug });
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

    private async Task<(Workspace? workspace, int userId, bool canView, bool canCreate, IActionResult? failure)> EnsureAccessAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return (null, 0, false, false, NotFound());

        if (!TryGetUserId(out var userId)) return (ws, 0, false, false, Forbid());

        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, ws.Id);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(ws.Id, userId);
        var canView = isAdmin || (eff.TryGetValue("users", out var up) && up.CanView);
        var canCreate = isAdmin || (eff.TryGetValue("users", out var up2) && up2.CanCreate);

        if (!canView || !canCreate) return (ws, userId, canView, canCreate, Forbid());

        return (ws, userId, canView, canCreate, null);
    }

    private static string GenerateStrongPassword(int length = 16)
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string symbols = "!@#$%^&*()-_=+[]{};:,.?";
        var all = upper + lower + digits + symbols;
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            var idx = bytes[i] % all.Length;
            chars[i] = all[idx];
        }
        return new string(chars);
    }
}
