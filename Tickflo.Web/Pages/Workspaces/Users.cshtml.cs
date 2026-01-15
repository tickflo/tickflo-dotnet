using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Utils;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class UsersModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IEmailSender _emailSender;
    private readonly INotificationRepository _notificationRepository;
    private readonly IWorkspaceUsersViewService _viewService;
    private readonly IWorkspaceUsersManageViewService _manageViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public UsersModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IEmailSender emailSender, INotificationRepository notificationRepository, IWorkspaceUsersViewService viewService, IWorkspaceUsersManageViewService manageViewService)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _emailSender = emailSender;
        _notificationRepository = notificationRepository;
        _viewService = viewService;
        _manageViewService = manageViewService;
    }

    public bool CanViewUsers { get; private set; }
    public bool CanCreateUsers { get; private set; }
    public bool CanEditUsers { get; private set; }
    
    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
        if (EnsurePermissionOrForbid(viewData.CanViewUsers) is IActionResult permCheck) return permCheck;
        
        IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        CanViewUsers = viewData.CanViewUsers;
        CanCreateUsers = viewData.CanCreateUsers;
        CanEditUsers = viewData.CanEditUsers;
        PendingInvites = viewData.PendingInvites;
        AcceptedUsers = viewData.AcceptedUsers;

        return Page();
    }

    public List<InviteView> PendingInvites { get; set; } = new();
    public List<AcceptedUserView> AcceptedUsers { get; set; } = new();
    public bool IsWorkspaceAdmin { get; set; }

    public async Task<IActionResult> OnPostAcceptAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;
        
        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;
        
        var uw = await _userWorkspaceRepo.FindAsync(userId, Workspace.Id);
        if (uw == null) return NotFound();
        uw.Accepted = true;
        uw.UpdatedAt = DateTime.UtcNow;
        await _userWorkspaceRepo.UpdateAsync(uw);
        SetSuccessMessage("Invite accepted.");
        return RedirectToPage("/Workspaces/Users", new { slug });
    }

    public async Task<IActionResult> OnPostResendAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;
        if (!TryGetUserId(out var currentUserId)) return Forbid();
        
        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;
        
        var uw = await _userWorkspaceRepo.FindAsync(userId, Workspace.Id);
        if (uw == null || uw.Accepted) return NotFound();
        var user = await _userRepo.FindByIdAsync(userId);
        if (EnsureEntityExistsOrNotFound(user) is IActionResult userCheck) return userCheck;
        var newCode = TokenGenerator.GenerateToken(16);
        user.EmailConfirmationCode = newCode;
        await _userRepo.UpdateAsync(user);
        var confirmationLink = $"/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(newCode)}";
        var subject = $"Your invite to {Workspace.Name}";
        var body = $"<p>Hello,</p><p>Here is your email confirmation link for workspace '{Workspace.Name}'.</p><p><a href=\"{confirmationLink}\">Confirm your email</a></p><p>Use your original temporary password to sign in.</p>";
        await _emailSender.SendAsync(user.Email, subject, body);
        
        // Create a notification record in the database
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = Workspace.Id,
            Type = "workspace_invite",
            DeliveryMethod = "email",
            Priority = "high",
            Subject = subject,
            Body = body,
            Status = "sent",
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId
        };
        
        await _notificationRepository.AddAsync(notification);
        
        TempData["Success"] = "Invite email resent.";
        return RedirectToPage("/Workspaces/Users", new { slug });
    }
}

