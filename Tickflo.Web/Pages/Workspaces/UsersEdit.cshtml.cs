using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class UsersEditModel : WorkspacePageModel
{
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
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;

        // Load user details
        var user = await _userRepo.FindByIdAsync(userId);
        if (user == null) return NotFound();

        UserName = user.Name ?? string.Empty;
        UserEmail = user.Email;
        IsAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(userId, Workspace.Id);
        
        // Load available roles and current roles
        AvailableRoles = await _roleRepo.ListForWorkspaceAsync(Workspace.Id);
        CurrentRoles = await _userWorkspaceRoleRepo.GetRolesAsync(userId, Workspace.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int userId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;

        // Find the Admin role
        var adminRole = await _roleRepo.FindByNameAsync(Workspace.Id, "Admin");
        if (adminRole == null)
        {
            SetErrorMessage("Admin role not found.");
            return RedirectToPage("/Workspaces/Users", new { slug });
        }

        // Check current admin status
        var currentRoles = await _userWorkspaceRoleRepo.GetRolesAsync(userId, Workspace.Id);
        var hasAdminRole = currentRoles.Any(r => r.Id == adminRole.Id);

        if (IsAdmin && !hasAdminRole)
        {
            // Add admin role
            await _userWorkspaceRoleRepo.AddAsync(userId, Workspace.Id, adminRole.Id, currentUserId);
            SetSuccessMessage("User promoted to admin.");
        }
        else if (!IsAdmin && hasAdminRole)
        {
            // Remove admin role
            await _userWorkspaceRoleRepo.RemoveAsync(userId, Workspace.Id, adminRole.Id);
            SetSuccessMessage("Admin privileges removed.");
        }
        else
        {
            SetSuccessMessage("No changes made.");
        }

        return RedirectToPage("/Workspaces/Users", new { slug });
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug, int userId, int roleId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;

        if (roleId <= 0)
        {
            SetErrorMessage("Please select a valid role.");
            return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        // Verify role belongs to workspace
        var role = await _roleRepo.FindByIdAsync(roleId);
        if (role == null || role.WorkspaceId != Workspace.Id)
        {
            SetErrorMessage("Invalid role selection.");
            return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        // Check if user already has this role
        var existingRoles = await _userWorkspaceRoleRepo.GetRolesAsync(userId, Workspace.Id);
        if (existingRoles.Any(r => r.Id == roleId))
        {
            SetErrorMessage("User already has this role.");
            return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
        }

        // Assign role
        await _userWorkspaceRoleRepo.AddAsync(userId, Workspace.Id, roleId, currentUserId);
        
        SetSuccessMessage("Role assigned successfully.");
        return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        WorkspaceSlug = slug;
        EditUserId = userId;
        
        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;
        
        var (workspace, currentUserId) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;

        var viewData = await _manageViewService.BuildAsync(Workspace.Id, currentUserId);
        if (EnsurePermissionOrForbid(viewData.CanEditUsers) is IActionResult permCheck) return permCheck;

        // Remove role
        await _userWorkspaceRoleRepo.RemoveAsync(userId, Workspace.Id, roleId);
        
        SetSuccessMessage("Role removed successfully.");
        return RedirectToPage("/Workspaces/UsersEdit", new { slug, userId });
    }
}
