using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class UsersEditModel : WorkspacePageModel
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

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IWorkspaceUsersManageViewService _manageViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string UserEmail { get; private set; } = string.Empty;
    public int EditUserId { get; private set; }
    public List<Role> AvailableRoles { get; set; } = new();
    public List<Role> CurrentRoles { get; set; } = new();

    [BindProperty]
    public bool IsAdmin { get; set; }

    public UsersEditModel(
        IWorkspaceRepository workspaceRepo,
        IUserRepository userRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IUserWorkspaceRoleRepository userWorkspaceRoleRepo,
        IRoleRepository roleRepo,
        IWorkspaceUsersManageViewService manageViewService)
    {
        _workspaceRepo = workspaceRepo;
        _userRepo = userRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _roleRepo = roleRepo;
        _manageViewService = manageViewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;

        var authCheck = await AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: false);
        if (authCheck is IActionResult result) return result;

        var user = await _userRepo.FindByIdAsync(userId);
        if (user == null) return NotFound();

        UserName = user.Name ?? string.Empty;
        UserEmail = user.Email;
        IsAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, Workspace!.Id);

        AvailableRoles = await _roleRepo.ListForWorkspaceAsync(Workspace.Id);
        CurrentRoles = await _userWorkspaceRoleRepo.GetRolesAsync(userId, Workspace.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;

        var authCheck = await AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: true);
        if (authCheck is IActionResult result) return result;

        await HandleAdminRoleChangeAsync(userId);
        return RedirectToPage("/Workspaces/Users", new { slug });
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug, int userId, int roleId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;

        var authCheck = await AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: true);
        if (authCheck is IActionResult result) return result;

        var roleValidation = await ValidateAndGetRoleAsync(roleId);
        if (roleValidation.IsError)
        {
            SetErrorMessage(roleValidation.ErrorMessage!);
            return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        var existingRoles = await _userWorkspaceRoleRepo.GetRolesAsync(userId, Workspace!.Id);
        if (existingRoles.Any(r => r.Id == roleId))
        {
            SetErrorMessage(UserAlreadyHasRoleError);
            return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        var currentUserId = TryGetUserId(out var uid) ? uid : 0;
        await _userWorkspaceRoleRepo.AddAsync(userId, Workspace.Id, roleId, currentUserId);
        SetSuccessMessage(RoleAssignedMessage);
        return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;

        var authCheck = await AuthorizeAndLoadWorkspaceAsync(slug, userId, isEditingRoles: true);
        if (authCheck is IActionResult result) return result;

        await _userWorkspaceRoleRepo.RemoveAsync(userId, Workspace!.Id, roleId);
        SetSuccessMessage(RoleRemovedMessage);
        return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    private async Task<IActionResult?> AuthorizeAndLoadWorkspaceAsync(string slug, int userId, bool isEditingRoles)
    {
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;

        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _manageViewService.BuildAsync(Workspace!.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;

        return null;
    }

    private async Task HandleAdminRoleChangeAsync(int userId)
    {
        var adminRole = await _roleRepo.FindByNameAsync(Workspace!.Id, AdminRoleName);
        if (adminRole == null)
        {
            SetErrorMessage(AdminRoleNotFound);
            return;
        }

        var currentRoles = await _userWorkspaceRoleRepo.GetRolesAsync(userId, Workspace.Id);
        var hasAdminRole = currentRoles.Any(r => r.Id == adminRole.Id);
        var currentUserId = TryGetUserId(out var uid) ? uid : 0;

        if (IsAdmin && !hasAdminRole)
        {
            await _userWorkspaceRoleRepo.AddAsync(userId, Workspace.Id, adminRole.Id, currentUserId);
            SetSuccessMessage(UserPromotedMessage);
        }
        else if (!IsAdmin && hasAdminRole)
        {
            await _userWorkspaceRoleRepo.RemoveAsync(userId, Workspace.Id, adminRole.Id);
            SetSuccessMessage(AdminPrivilegesRemovedMessage);
        }
        else
        {
            SetSuccessMessage(NoChangesMessage);
        }
    }

    private async Task<(bool IsError, string? ErrorMessage)> ValidateAndGetRoleAsync(int roleId)
    {
        if (roleId <= InvalidRoleId)
            return (true, InvalidRoleError);

        var role = await _roleRepo.FindByIdAsync(roleId);
        if (role == null || role.WorkspaceId != Workspace!.Id)
            return (true, InvalidRoleSelectionError);

        return (false, null);
    }
}
