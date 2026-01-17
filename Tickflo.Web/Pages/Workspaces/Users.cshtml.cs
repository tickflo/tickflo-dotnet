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
    #region Constants
    private const string InviteAcceptedMessage = "Invite accepted.";
    private const string InviteEmailResentMessage = "Invite email resent.";
    private const string WorkspaceInviteNotificationType = "workspace_invite";
    private const string EmailDeliveryMethod = "email";
    private const string HighPriority = "high";
    private const string SentStatus = "sent";
    #endregion

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly INotificationRepository _notificationRepository;
    private readonly IWorkspaceUsersViewService _viewService;
    private readonly IWorkspaceUsersManageViewService _manageViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public UsersModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IEmailSender emailSender, IEmailTemplateService emailTemplateService, INotificationRepository notificationRepository, IWorkspaceUsersViewService viewService, IWorkspaceUsersManageViewService manageViewService)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _emailSender = emailSender;
        _emailTemplateService = emailTemplateService;
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

        var viewData = await _viewService.BuildAsync(Workspace!.Id, uid);
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
        
        if (await AuthorizeWorkspaceAccessAsync(slug) is IActionResult authResult)
            return authResult;
        
        var uw = await _userWorkspaceRepo.FindAsync(userId, Workspace!.Id);
        if (uw == null) return NotFound();
        
        AcceptUserInvite(uw);
        await _userWorkspaceRepo.UpdateAsync(uw);
        
        SetSuccessMessage(InviteAcceptedMessage);
        return RedirectToUsersPage(slug);
    }

    public async Task<IActionResult> OnPostResendAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(Workspace) is IActionResult result) return result;
        if (!TryGetUserId(out var currentUserId)) return Forbid();
        
        var viewData = await _manageViewService.BuildAsync(Workspace!.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;
        
        var uw = await _userWorkspaceRepo.FindAsync(userId, Workspace.Id);
        if (uw == null || uw.Accepted) return NotFound();
        
        var user = await _userRepo.FindByIdAsync(userId);
        if (EnsureEntityExistsOrNotFound(user) is IActionResult userCheck) return userCheck;
        
        await RegenerateConfirmationCodeAsync(user!);
        await SendInviteEmailAsync(user!, Workspace, BuildConfirmationLink(user!));
        await CreateNotificationRecordAsync(userId, Workspace.Id, currentUserId);
        
        TempData["Success"] = InviteEmailResentMessage;
        return RedirectToUsersPage(slug);
    }

    private async Task<IActionResult?> AuthorizeWorkspaceAccessAsync(string slug)
    {
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult)
            return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _manageViewService.BuildAsync(Workspace!.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck)
            return permCheck;
        
        return null;
    }

    private void AcceptUserInvite(UserWorkspace userWorkspace)
    {
        userWorkspace.Accepted = true;
        userWorkspace.UpdatedAt = DateTime.UtcNow;
    }

    private async Task RegenerateConfirmationCodeAsync(User user)
    {
        var newCode = TokenGenerator.GenerateToken(16);
        user.EmailConfirmationCode = newCode;
        await _userRepo.UpdateAsync(user);
    }

    private string BuildConfirmationLink(User user)
    {
        return $"/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(user.EmailConfirmationCode ?? string.Empty)}";
    }

    private async Task SendInviteEmailAsync(User user, Workspace workspace, string confirmationLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "WORKSPACE_NAME", workspace.Name },
            { "CONFIRMATION_LINK", confirmationLink }
        };
        
        var (subject, body) = await _emailTemplateService.RenderTemplateAsync(EmailTemplateType.WorkspaceInviteResend, variables, workspace.Id);
        await _emailSender.SendAsync(user.Email, subject, body);
    }

    private async Task CreateNotificationRecordAsync(int userId, int workspaceId, int createdBy)
    {
        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = WorkspaceInviteNotificationType,
            DeliveryMethod = EmailDeliveryMethod,
            Priority = HighPriority,
            Subject = string.Empty,
            Body = string.Empty,
            Status = SentStatus,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        
        await _notificationRepository.AddAsync(notification);
    }

    private IActionResult RedirectToUsersPage(string slug)
    {
        return RedirectToPage("/Workspaces/Users", new { slug });
    }
}

