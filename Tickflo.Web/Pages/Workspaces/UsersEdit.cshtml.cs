namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;

[Authorize]
public class UsersEditModel(
    IWorkspaceRepository workspaceRepo,
    IUserRepository userRepo,
    IUserWorkspaceRepository userWorkspaceRepo,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRoleRepository roleRepo,
    IWorkspaceUsersManageViewService manageViewService) : WorkspacePageModel
{
    #region Constants
    private const int InvalidRoleId = 0;
    private const string AdminRoleName = "Admin";
    private const string AdminRoleNotFound = "Admin role not found.";
    private const string UserPromotedMessage = "User promoted to admin.";
    private const string AdminPrivilegesRemovedMessage = "Admin privileges removed.";
    private const string NoChangesMessage = "No changes made.";
    private const string InvalidRoleError = "Please select a valid role.";
    private const string InvalidRoleSelectionError = "Invalid role selection.";
    private const string UserAlreadyHasRoleError = "User already has this role.";
    private const string RoleAssignedMessage = "Role assigned successfully.";
    private const string RoleRemovedMessage = "Role removed successfully.";
    #endregion

    private readonly IWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo = userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
    private readonly IRoleRepository _roleRepo = roleRepo;
    private readonly IWorkspaceUsersManageViewService _manageViewService = manageViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public int EditUserId { get; private set; }
    public List<Role> AvailableRoles { get; set; } = [];
    public List<Role> CurrentRoles { get; set; } = [];

    [BindProperty]
    public bool IsAdmin { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int userId)
    {
        this.WorkspaceSlug = slug;
        this.EditUserId = userId;

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: false);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        var user = await this._userRepo.FindByIdAsync(userId);
        if (user == null)
        {
            return this.NotFound();
        }

        this.UserName = user.Name ?? string.Empty;
        this.UserEmail = user.Email;
        this.IsAdmin = await this._userWorkspaceRoleRepo.IsAdminAsync(userId, this.Workspace!.Id);

        this.AvailableRoles = await this._roleRepo.ListForWorkspaceAsync(this.Workspace.Id);
        this.CurrentRoles = await this._userWorkspaceRoleRepo.GetRolesAsync(userId, this.Workspace.Id);

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int userId)
    {
        this.WorkspaceSlug = slug;
        this.EditUserId = userId;

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: true);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        await this.HandleAdminRoleChangeAsync(userId);
        return this.RedirectToPage("/Workspaces/Users", new { slug });
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug, int userId, int roleId)
    {
        this.WorkspaceSlug = slug;
        this.EditUserId = userId;

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: true);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        var (IsError, ErrorMessage) = await this.ValidateAndGetRoleAsync(roleId);
        if (IsError)
        {
            this.SetErrorMessage(ErrorMessage!);
            return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        var existingRoles = await this._userWorkspaceRoleRepo.GetRolesAsync(userId, this.Workspace!.Id);
        if (existingRoles.Any(r => r.Id == roleId))
        {
            this.SetErrorMessage(UserAlreadyHasRoleError);
            return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        var currentUserId = this.TryGetUserId(out var uid) ? uid : 0;
        await this._userWorkspaceRoleRepo.AddAsync(userId, this.Workspace.Id, roleId, currentUserId);
        this.SetSuccessMessage(RoleAssignedMessage);
        return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        this.WorkspaceSlug = slug;
        this.EditUserId = userId;

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: true);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        await this._userWorkspaceRoleRepo.RemoveAsync(userId, this.Workspace!.Id, roleId);
        this.SetSuccessMessage(RoleRemovedMessage);
        return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    private async Task<IActionResult?> AuthorizeAndLoadWorkspaceAsync(string slug, int userId, bool isEditingRoles)
    {
        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this._workspaceRepo, this._userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;

        var viewData = await this._manageViewService.BuildAsync(this.Workspace!.Id, currentUserId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        return null;
    }

    private async Task HandleAdminRoleChangeAsync(int userId)
    {
        var adminRole = await this._roleRepo.FindByNameAsync(this.Workspace!.Id, AdminRoleName);
        if (adminRole == null)
        {
            this.SetErrorMessage(AdminRoleNotFound);
            return;
        }

        var currentRoles = await this._userWorkspaceRoleRepo.GetRolesAsync(userId, this.Workspace.Id);
        var hasAdminRole = currentRoles.Any(r => r.Id == adminRole.Id);
        var currentUserId = this.TryGetUserId(out var uid) ? uid : 0;

        if (this.IsAdmin && !hasAdminRole)
        {
            await this._userWorkspaceRoleRepo.AddAsync(userId, this.Workspace.Id, adminRole.Id, currentUserId);
            this.SetSuccessMessage(UserPromotedMessage);
        }
        else if (!this.IsAdmin && hasAdminRole)
        {
            await this._userWorkspaceRoleRepo.RemoveAsync(userId, this.Workspace.Id, adminRole.Id);
            this.SetSuccessMessage(AdminPrivilegesRemovedMessage);
        }
        else
        {
            this.SetSuccessMessage(NoChangesMessage);
        }
    }

    private async Task<(bool IsError, string? ErrorMessage)> ValidateAndGetRoleAsync(int roleId)
    {
        if (roleId <= InvalidRoleId)
        {
            return (true, InvalidRoleError);
        }

        var role = await this._roleRepo.FindByIdAsync(roleId);
        if (role == null || role.WorkspaceId != this.Workspace!.Id)
        {
            return (true, InvalidRoleSelectionError);
        }

        return (false, null);
    }
}
