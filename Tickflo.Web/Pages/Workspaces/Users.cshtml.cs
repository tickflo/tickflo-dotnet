namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;
using Tickflo.Core.Utils;

[Authorize]
public class UsersModel(IWorkspaceService workspaceService, IUserRepository userRepository, IUserWorkspaceRepository userWorkspaceRepository, IEmailSenderService emailSenderService, IEmailTemplateService emailTemplateService, INotificationRepository notificationRepository, IWorkspaceUsersViewService workspaceUsersViewService, IWorkspaceUsersManageViewService workspaceUsersManageViewService) : WorkspacePageModel
{
    #region Constants
    private const string InviteAcceptedMessage = "Invite accepted.";
    private const string InviteEmailResentMessage = "Invite email resent.";
    private const string WorkspaceInviteNotificationType = "workspace_invite";
    private const string EmailDeliveryMethod = "email";
    private const string HighPriority = "high";
    private const string SentStatus = "sent";
    #endregion

    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IEmailSenderService emailSenderService = emailSenderService;
    private readonly IEmailTemplateService emailTemplateService = emailTemplateService;
    private readonly INotificationRepository notificationRepository = notificationRepository;
    private readonly IWorkspaceUsersViewService workspaceUsersViewService = workspaceUsersViewService;
    private readonly IWorkspaceUsersManageViewService workspaceUsersManageViewService = workspaceUsersManageViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public bool CanViewUsers { get; private set; }
    public bool CanCreateUsers { get; private set; }
    public bool CanEditUsers { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceUsersViewService.BuildAsync(this.Workspace!.Id, uid);
        if (this.EnsurePermissionOrForbid(viewData.CanViewUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.IsWorkspaceAdmin = viewData.IsWorkspaceAdmin;
        this.CanViewUsers = viewData.CanViewUsers;
        this.CanCreateUsers = viewData.CanCreateUsers;
        this.CanEditUsers = viewData.CanEditUsers;
        this.PendingInvites = viewData.PendingInvites;
        this.AcceptedUsers = viewData.AcceptedUsers;

        return this.Page();
    }

    public List<InviteView> PendingInvites { get; set; } = [];
    public List<AcceptedUserView> AcceptedUsers { get; set; } = [];
    public bool IsWorkspaceAdmin { get; set; }

    public async Task<IActionResult> OnPostAcceptAsync(string slug, int userId)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeWorkspaceAccessAsync(slug) is IActionResult authResult)
        {
            return authResult;
        }

        var userWorkspace = await this.userWorkspaceRepository.FindAsync(userId, this.Workspace!.Id);
        if (userWorkspace == null)
        {
            return this.NotFound();
        }

        AcceptUserInvite(userWorkspace);
        await this.userWorkspaceRepository.UpdateAsync(userWorkspace);

        this.SetSuccessMessage(InviteAcceptedMessage);
        return this.RedirectToUsersPage(slug);
    }

    public async Task<IActionResult> OnPostResendAsync(string slug, int userId)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(this.Workspace) is IActionResult result)
        {
            return result;
        }

        if (!this.TryGetUserId(out var currentUserId))
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceUsersManageViewService.BuildAsync(this.Workspace!.Id, currentUserId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        var userWorkspace = await this.userWorkspaceRepository.FindAsync(userId, this.Workspace.Id);
        if (userWorkspace == null || userWorkspace.Accepted)
        {
            return this.NotFound();
        }

        var user = await this.userRepository.FindByIdAsync(userId);
        if (this.EnsureEntityExistsOrNotFound(user) is IActionResult userCheck)
        {
            return userCheck;
        }

        await this.RegenerateConfirmationCodeAsync(user!);
        await this.SendInviteEmailAsync(user!, this.Workspace, BuildConfirmationLink(user!));
        await this.CreateNotificationRecordAsync(userId, this.Workspace.Id, currentUserId);

        this.TempData["Success"] = InviteEmailResentMessage;
        return this.RedirectToUsersPage(slug);
    }

    private async Task<IActionResult?> AuthorizeWorkspaceAccessAsync(string slug)
    {
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var currentUserId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(currentUserId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceUsersManageViewService.BuildAsync(this.Workspace.Id, currentUserId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        return null;
    }

    private static void AcceptUserInvite(UserWorkspace userWorkspace)
    {
        userWorkspace.Accepted = true;
        userWorkspace.UpdatedAt = DateTime.UtcNow;
    }

    private async Task RegenerateConfirmationCodeAsync(User user)
    {
        var newCode = TokenGenerator.GenerateToken(16);
        user.EmailConfirmationCode = newCode;
        await this.userRepository.UpdateAsync(user);
    }

    private static string BuildConfirmationLink(User user) => $"/email-confirmation/confirm?email={Uri.EscapeDataString(user.Email)}&code={Uri.EscapeDataString(user.EmailConfirmationCode ?? string.Empty)}";

    private async Task SendInviteEmailAsync(User user, Workspace workspace, string confirmationLink)
    {
        var variables = new Dictionary<string, string>
        {
            { "WORKSPACE_NAME", workspace.Name },
            { "CONFIRMATION_LINK", confirmationLink }
        };

        var (subject, body) = await this.emailTemplateService.RenderTemplateAsync(EmailTemplateType.WorkspaceInviteResend, variables, workspace.Id);
        await this.emailSenderService.SendAsync(user.Email, subject, body);
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

        await this.notificationRepository.AddAsync(notification);
    }

    private RedirectToPageResult RedirectToUsersPage(string slug) => this.RedirectToPage("/Workspaces/Users", new { slug });
}

