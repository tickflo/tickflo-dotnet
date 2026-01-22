namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;

[Authorize]
public class UsersEditModel(
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
    IRoleRepository roleRepo,
    IWorkspaceUsersManageViewService workspaceUsersManageViewService) : WorkspacePageModel
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

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IUserRepository userRepository = userRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepo;
    private readonly IRoleRepository roleRepository = roleRepo;
    private readonly IWorkspaceUsersManageViewService workspaceUsersManageViewService = workspaceUsersManageViewService;

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

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        var user = await this.userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return this.NotFound();
        }

        this.UserName = user.Name ?? string.Empty;
        this.UserEmail = user.Email;
        this.IsAdmin = await this.userWorkspaceRoleRepository.IsAdminAsync(userId, this.Workspace!.Id);

        this.AvailableRoles = await this.roleRepository.ListForWorkspaceAsync(this.Workspace.Id);
        this.CurrentRoles = await this.userWorkspaceRoleRepository.GetRolesAsync(userId, this.Workspace.Id);

        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int userId)
    {
        this.WorkspaceSlug = slug;
        this.EditUserId = userId;

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug);
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

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        var (isError, errorMessage) = await this.ValidateAndGetRoleAsync(roleId);
        if (isError)
        {
            this.SetErrorMessage(errorMessage!);
            return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        var existingRoles = await this.userWorkspaceRoleRepository.GetRolesAsync(userId, this.Workspace!.Id);
        if (existingRoles.Any(r => r.Id == roleId))
        {
            this.SetErrorMessage(UserAlreadyHasRoleError);
            return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        var currentUserId = this.TryGetUserId(out var uid) ? uid : 0;
        await this.userWorkspaceRoleRepository.AddAsync(userId, this.Workspace.Id, roleId, currentUserId);
        this.SetSuccessMessage(RoleAssignedMessage);
        return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        this.WorkspaceSlug = slug;
        this.EditUserId = userId;

        var authCheck = await this.AuthorizeAndLoadWorkspaceAsync(slug);
        if (authCheck is IActionResult result)
        {
            return result;
        }

        await this.userWorkspaceRoleRepository.RemoveAsync(userId, this.Workspace!.Id, roleId);
        this.SetSuccessMessage(RoleRemovedMessage);
        return this.RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    private async Task<IActionResult?> AuthorizeAndLoadWorkspaceAsync(string slug)
    {
        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;

        var viewData = await this.workspaceUsersManageViewService.BuildAsync(this.Workspace!.Id, currentUserId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck)
        {
            return permCheck;
        }

        return null;
    }

    private async Task HandleAdminRoleChangeAsync(int userId)
    {
        var adminRole = await this.roleRepository.FindByNameAsync(this.Workspace!.Id, AdminRoleName);
        if (adminRole == null)
        {
            this.SetErrorMessage(AdminRoleNotFound);
            return;
        }

        var currentRoles = await this.userWorkspaceRoleRepository.GetRolesAsync(userId, this.Workspace.Id);
        var hasAdminRole = currentRoles.Any(r => r.Id == adminRole.Id);
        var currentUserId = this.TryGetUserId(out var uid) ? uid : 0;

        if (this.IsAdmin && !hasAdminRole)
        {
            await this.userWorkspaceRoleRepository.AddAsync(userId, this.Workspace.Id, adminRole.Id, currentUserId);
            this.SetSuccessMessage(UserPromotedMessage);
        }
        else if (!this.IsAdmin && hasAdminRole)
        {
            await this.userWorkspaceRoleRepository.RemoveAsync(userId, this.Workspace.Id, adminRole.Id);
            this.SetSuccessMessage(AdminPrivilegesRemovedMessage);
        }
        else
        {
            this.SetSuccessMessage(NoChangesMessage);
        }
    }

    private async Task<(bool isError, string? errorMessage)> ValidateAndGetRoleAsync(int roleId)
    {
        if (roleId <= InvalidRoleId)
        {
            return (true, InvalidRoleError);
        }

        var role = await this.roleRepository.FindByIdAsync(roleId);
        if (role == null || role.WorkspaceId != this.Workspace!.Id)
        {
            return (true, InvalidRoleSelectionError);
        }

        return (false, null);
    }
}
