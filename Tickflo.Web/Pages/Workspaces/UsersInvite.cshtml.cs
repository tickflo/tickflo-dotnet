using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
// using Tickflo.Core.Data; // removed duplicate using
using Tickflo.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;
using System.Security.Cryptography;
using Tickflo.Core.Data;
using Tickflo.Core.Services;
using Microsoft.AspNetCore.Authorization;

using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Users;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class UsersInviteModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly ITokenRepository _tokenRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IUserInvitationService _invitationService;
    private readonly IWorkspaceUsersInviteViewService _viewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [BindProperty]
    public string Role { get; set; } = "Member";

    public UsersInviteModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IPasswordHasher passwordHasher, IEmailSender emailSender, ITokenRepository tokenRepo, IRoleRepository roleRepo, IUserInvitationService invitationService, IWorkspaceUsersInviteViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _tokenRepo = tokenRepo;
        _roleRepo = roleRepo;
        _invitationService = invitationService;
        _viewService = viewService;
    }
    public bool CanViewUsers { get; private set; }
    public bool CanCreateUsers { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;

        if (!TryGetUserId(out var userId)) return Forbid();
        var viewData = await _viewService.BuildAsync(Workspace.Id, userId);
        if (EnsurePermissionOrForbid(viewData.CanViewUsers && viewData.CanCreateUsers) is IActionResult permCheck) return permCheck;

        CanViewUsers = viewData.CanViewUsers;
        CanCreateUsers = viewData.CanCreateUsers;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!TryGetUserId(out var currentUserId)) return Forbid();
        var viewData = await _viewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanViewUsers && viewData.CanCreateUsers) is IActionResult permCheck) return permCheck;

        CanViewUsers = viewData.CanViewUsers;
        CanCreateUsers = viewData.CanCreateUsers;

        var ws = Workspace;
        if (ws == null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Resolve role IDs if a role name was provided
            List<int>? roleIds = null;
            var selectedRoleName = Role?.Trim();
            if (!string.IsNullOrWhiteSpace(selectedRoleName))
            {
                var role = await _roleRepo.FindByNameAsync(ws.Id, selectedRoleName);
                if (role == null)
                {
                    var adminFlag = string.Equals(selectedRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
                    role = await _roleRepo.AddAsync(ws.Id, selectedRoleName, adminFlag, currentUserId);
                }
                roleIds = new List<int> { role.Id };
            }

            var result = await _invitationService.InviteUserAsync(ws.Id, Email.Trim(), currentUserId, roleIds);

            // Compose and send email using provided links
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var confirmationLink = baseUrl + result.ConfirmationLink;
            var acceptLink = baseUrl + result.AcceptLink;
            var setPasswordLink = baseUrl + result.ResetPasswordLink;
            var subject = $"You're invited to {ws.Name}";
            var body = $"<div style='font-family:Arial,sans-serif'>"+
                        $"<h2 style='color:#333'>Workspace Invitation</h2>"+
                        $"<p>You have been invited to the workspace '<b>{ws.Name}</b>'.</p>"+
                        $"<p>Temporary password: <code style='font-size:1.1em'>{result.TemporaryPassword}</code></p>"+
                        $"<p>Please confirm your email: <a href=\"{confirmationLink}\">Confirm Email</a></p>"+
                        $"<p>Then accept the invite: <a href=\"{acceptLink}\">Accept Invite</a></p>"+
                        $"<p>Or set your password now: <a href=\"{setPasswordLink}\">Set Password</a></p>"+
                        $"<hr/><p style='color:#777'>If you did not expect this email, you can ignore it.</p>"+
                        $"</div>";
            await _emailSender.SendAsync(result.User.Email!, subject, body);

            SetSuccessMessage($"Invite created for '{Email}'" + (!string.IsNullOrWhiteSpace(Role) ? $" as {Role}" : "") + ".");
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
            return Page();
        }
        return RedirectToPage("/Workspaces/Users", new { slug });
    }
}


