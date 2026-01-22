namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Roles;
using Tickflo.Core.Services.Views;

[Authorize]
public class RolesAssignModel(
    IWorkspaceRepository workspaceRepository,
    IRoleManagementService roleManagementService,
    IWorkspaceRolesAssignViewService workspaceRolesAssignViewService) : WorkspacePageModel
{
    #region Constants
    private const string SelectUserAndRoleError = "Please select both a user and a role.";
    private const string InvalidRoleSelectionError = "Invalid role selection.";
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IRoleManagementService roleManagementService = roleManagementService;
    private readonly IWorkspaceRolesAssignViewService workspaceRolesAssignViewService = workspaceRolesAssignViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<User> Members { get; private set; } = [];
    public List<Role> Roles { get; private set; } = [];
    public Dictionary<int, List<Role>> UserRoles { get; private set; } = [];

    [BindProperty]
    public int SelectedUserId { get; set; }
    [BindProperty]
    public int SelectedRoleId { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        if (await this.AuthorizeAndLoadWorkspaceAsync(slug) is IActionResult authResult)
        {
            return authResult;
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        var ws = await this.workspaceRepository.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        var uid = this.GetUserIdOrZero();
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this.workspaceRolesAssignViewService.BuildAsync(ws!.Id, uid);
        if (!data.IsAdmin)
        {
            return this.Forbid();
        }

        if (!this.ValidateRoleAssignmentInput())
        {
            return await this.OnGetAsync(slug);
        }

        if (!await this.roleManagementService.RoleBelongsToWorkspaceAsync(this.SelectedRoleId, ws!.Id))
        {
            this.ModelState.AddModelError(string.Empty, InvalidRoleSelectionError);
            return await this.OnGetAsync(slug);
        }

        try
        {
            await this.roleManagementService.AssignRoleToUserAsync(this.SelectedUserId, ws!.Id, this.SelectedRoleId, uid);
        }
        catch (InvalidOperationException ex)
        {
            this.ModelState.AddModelError(string.Empty, ex.Message);
            return await this.OnGetAsync(slug);
        }

        return this.RedirectToRolesAssignPage(slug);
    }

    public async Task<IActionResult> OnPostRemoveAsync(string slug, int userId, int roleId)
    {
        this.WorkspaceSlug = slug;
        var ws = await this.workspaceRepository.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        var uid = this.GetUserIdOrZero();
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this.workspaceRolesAssignViewService.BuildAsync(ws!.Id, uid);
        if (!data.IsAdmin)
        {
            return this.Forbid();
        }

        await this.roleManagementService.RemoveRoleFromUserAsync(userId, ws!.Id, roleId);
        return this.RedirectToRolesAssignPage(slug);
    }

    private async Task<IActionResult?> AuthorizeAndLoadWorkspaceAsync(string slug)
    {
        var ws = await this.workspaceRepository.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        var uid = this.GetUserIdOrZero();
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this.workspaceRolesAssignViewService.BuildAsync(ws!.Id, uid);
        if (!data.IsAdmin)
        {
            return this.Forbid();
        }

        this.Members = data.Members ?? [];
        this.Roles = data.Roles ?? [];
        this.UserRoles = data.UserRoles ?? [];

        return null;
    }

    private int GetUserIdOrZero() => this.TryGetUserId(out var idVal) ? idVal : 0;

    private bool ValidateRoleAssignmentInput()
    {
        if (this.SelectedUserId <= 0 || this.SelectedRoleId <= 0)
        {
            this.ModelState.AddModelError(string.Empty, SelectUserAndRoleError);
            return false;
        }
        return true;
    }

    private RedirectResult RedirectToRolesAssignPage(string slug)
    {
        var queryQ = this.Request.Query["Query"].ToString();
        return this.Redirect($"/workspaces/{slug}/users/roles/assign?Query={Uri.EscapeDataString(queryQ ?? string.Empty)}");
    }
}

