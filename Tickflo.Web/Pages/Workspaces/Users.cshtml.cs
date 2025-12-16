using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;
using System.Security.Claims;

namespace Tickflo.Web.Pages.Workspaces;

public class UsersModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IEmailSender _emailSender;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public UsersModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IEmailSender emailSender, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _emailSender = emailSender;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null)
        {
            var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(uidStr, out var uid))
            {
                IsWorkspaceAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
            }
            var memberships = await _userWorkspaceRepo.FindForWorkspaceAsync(Workspace.Id);
            PendingInvites = new List<InviteView>();
            foreach (var m in memberships.Where(m => !m.Accepted))
            {
                var u = await _userRepo.FindByIdAsync(m.UserId);
                if (u == null) continue;
                var roles = await _userWorkspaceRoleRepo.GetRoleNamesAsync(u.Id, Workspace.Id);
                PendingInvites.Add(new InviteView
                {
                    UserId = u.Id,
                    Email = u.Email,
                    CreatedAt = m.CreatedAt,
                    Roles = roles
                });
            }
        }
    }

    public List<InviteView> PendingInvites { get; set; } = new();
    public bool IsWorkspaceAdmin { get; set; }

    public class InviteView
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public async Task<IActionResult> OnPostAcceptAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var currentUserId)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(currentUserId, Workspace.Id);
        if (!isAdmin) return Forbid();
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
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var currentUserId)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(currentUserId, Workspace.Id);
        if (!isAdmin) return Forbid();
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
}
