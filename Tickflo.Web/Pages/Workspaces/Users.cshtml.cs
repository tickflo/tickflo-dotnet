using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class UsersModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IEmailSender _emailSender;
    private readonly IWorkspaceUsersViewService _viewService;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IWorkspaceUsersManageViewService _manageViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public UsersModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IEmailSender emailSender, IWorkspaceUsersViewService viewService, IRolePermissionRepository rolePerms, IWorkspaceUsersManageViewService manageViewService)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _emailSender = emailSender;
        _viewService = viewService;
        _rolePerms = rolePerms;
        _manageViewService = manageViewService;
    }

    public bool CanCreateUsers { get; private set; }
    public bool CanEditUsers { get; private set; }
    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null)
        {
            if (TryGetUserId(out var uid))
            {
                var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
                IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
                CanCreateUsers = viewData.CanCreateUsers;
                CanEditUsers = viewData.CanEditUsers;
                PendingInvites = viewData.PendingInvites;
            }
        }
    }

    public List<InviteView> PendingInvites { get; set; } = new();
    public bool IsWorkspaceAdmin { get; set; }

    public async Task<IActionResult> OnPostAcceptAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var currentUserId)) return Forbid();
        
        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (!viewData.CanEditUsers) return Forbid();
        
        var uw = await _userWorkspaceRepo.FindAsync(userId, Workspace.Id);
        if (uw == null) return NotFound();
        uw.Accepted = true;
        uw.UpdatedAt = DateTime.UtcNow;
        await _userWorkspaceRepo.UpdateAsync(uw);
        TempData["Success"] = "Invite accepted.";
        return RedirectToPage("/Workspaces/Users", new { slug });
    }

    public async Task<IActionResult> OnPostResendAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var currentUserId)) return Forbid();
        
        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (!viewData.CanEditUsers) return Forbid();
        
        var uw = await _userWorkspaceRepo.FindAsync(userId, Workspace.Id);
        if (uw == null || uw.Accepted) return NotFound();
        var user = await _userRepo.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var newCode = TokenGenerator.GenerateToken(16);
        user.EmailConfirmationCode = newCode;
        await _userRepo.UpdateAsync(user);
        var confirmationLink = $"/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(newCode)}";
        var subject = $"Your invite to {Workspace.Name}";
        var body = $"<p>Hello,</p><p>Here is your email confirmation link for workspace '{Workspace.Name}'.</p><p><a href=\"{confirmationLink}\">Confirm your email</a></p><p>Use your original temporary password to sign in.</p>";
        await _emailSender.SendAsync(user.Email, subject, body);
        TempData["Success"] = "Invite email resent.";
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
}
