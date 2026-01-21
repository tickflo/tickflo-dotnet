namespace Tickflo.Web.Pages.Workspaces;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;
using Tickflo.Core.Services.Email;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;

[Authorize]
public class UsersInviteModel(IWorkspaceRepository workspaceRepo, IUserRepository userRepository, IUserWorkspaceRepository userWorkspaceRepository, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IPasswordHasher passwordHasher, IEmailSenderService emailSender, INotificationRepository notificationRepository, ITokenRepository tokenRepo, IRoleRepository roleRepo, IUserInvitationService invitationService, IWorkspaceUsersInviteViewService viewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IEmailSenderService _emailSender = emailSender;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly ITokenRepository _tokenRepo = tokenRepo;
    private readonly IRoleRepository roleRepository = roleRepo;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IUserInvitationService _invitationService = invitationService;
    private readonly IWorkspaceUsersInviteViewService _viewService = viewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [BindProperty]
    public string Role { get; set; } = "Member";
    public bool CanViewUsers { get; private set; }
    public bool CanCreateUsers { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, userId) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, userId);
        if (this.EnsurePermissionOrForbid(viewData.CanViewUsers && viewData.CanCreateUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.CanViewUsers = viewData.CanViewUsers;
        this.CanCreateUsers = viewData.CanCreateUsers;
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;

        var viewData = await this._viewService.BuildAsync(this.Workspace!.Id, currentUserId);
        if (this.EnsurePermissionOrForbid(viewData.CanViewUsers && viewData.CanCreateUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.CanViewUsers = viewData.CanViewUsers;
        this.CanCreateUsers = viewData.CanCreateUsers;

        var ws = this.Workspace;
        if (ws == null)
        {
            return this.Forbid();
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        try
        {
            List<int>? roleIds = null;
            var selectedRoleName = this.Role?.Trim();
            if (!string.IsNullOrWhiteSpace(selectedRoleName))
            {
                var role = await this.roleRepository.FindByNameAsync(ws.Id, selectedRoleName);
                if (role == null)
                {
                    var adminFlag = string.Equals(selectedRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
                    role = await this.roleRepository.AddAsync(ws.Id, selectedRoleName, adminFlag, currentUserId);
                }
                roleIds = [role.Id];
            }

            var result = await this._invitationService.InviteUserAsync(ws.Id, this.Email.Trim(), currentUserId, roleIds);

            var baseUrl = $"{this.Request.Scheme}://{this.Request.Host}";
            var confirmationLink = baseUrl + result.ConfirmationLink;
            var acceptLink = baseUrl + result.AcceptLink;
            var setPasswordLink = baseUrl + result.ResetPasswordLink;

            // Template Type ID 2 = Workspace Invite (new user)
            var variables = new Dictionary<string, string>
            {
                { "WORKSPACE_NAME", ws.Name },
                { "TEMPORARY_PASSWORD", result.TemporaryPassword },
                { "CONFIRMATION_LINK", confirmationLink },
                { "ACCEPT_LINK", acceptLink },
                { "SET_PASSWORD_LINK", setPasswordLink }
            };

            var (subject, body) = await this._emailTemplateService.RenderTemplateAsync(EmailTemplateType.WorkspaceInviteNewUser, variables, ws.Id);
            await this._emailSender.SendAsync(result.User.Email!, subject, body);

            // Create a notification record in the database
            var notification = new Notification
            {
                UserId = result.User.Id,
                WorkspaceId = ws.Id,
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

            await this._notificationRepository.AddAsync(notification);

            this.SetSuccessMessage($"Invite created for '{this.Email}'" + (!string.IsNullOrWhiteSpace(this.Role) ? $" as {this.Role}" : "") + ".");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
            return this.Page();
        }
        return this.RedirectToPage("/Workspaces/Users", new { slug });
    }
}


