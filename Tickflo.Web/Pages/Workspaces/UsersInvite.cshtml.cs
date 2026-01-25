namespace Tickflo.Web.Pages.Workspaces;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class UsersInviteModel(IWorkspaceService workspaceService, IRoleRepository roleRepository, IUserInvitationService userInvitationService, IWorkspaceUsersInviteViewService workspaceUsersInviteViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IUserInvitationService userInvitationService = userInvitationService;
    private readonly IWorkspaceUsersInviteViewService workspaceUsersInviteViewService = workspaceUsersInviteViewService;
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
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(userId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceUsersInviteViewService.BuildAsync(this.Workspace.Id, userId);
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

        var viewData = await this.workspaceUsersInviteViewService.BuildAsync(this.Workspace.Id, currentUserId);
        if (this.EnsurePermissionOrForbid(viewData.CanViewUsers && viewData.CanCreateUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.CanViewUsers = viewData.CanViewUsers;
        this.CanCreateUsers = viewData.CanCreateUsers;

        var workspace = this.Workspace;
        if (workspace == null)
        {
            return this.Forbid();
        }

        if (!this.ModelState.IsValid)
        {
            return this.Page();
        }

        try
        {
            List<int> roleIds;
            var selectedRoleName = this.Role?.Trim();
            if (!string.IsNullOrWhiteSpace(selectedRoleName))
            {
                var role = await this.roleRepository.FindByNameAsync(workspace.Id, selectedRoleName);
                if (role == null)
                {
                    var adminFlag = string.Equals(selectedRoleName, "Admin", StringComparison.OrdinalIgnoreCase);
                    role = await this.roleRepository.AddAsync(new Role
                    {
                        WorkspaceId = workspace.Id,
                        Name = selectedRoleName,
                        Admin = adminFlag,
                    });
                }
                roleIds = [role.Id];
            }
            else
            {
                this.SetErrorMessage("Role is required");
                return this.Page();
            }

            await this.userInvitationService.InviteUserAsync(workspace.Id, this.Email.Trim(), currentUserId, roleIds);

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


