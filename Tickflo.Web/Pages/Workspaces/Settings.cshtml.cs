namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;
using Tickflo.Web.Helpers;

[Authorize]
public partial class SettingsModel(IWorkspaceService workspaceService, IWorkspaceSettingsService workspaceSettingsService, IWorkspaceSettingsViewService workspaceSettingsViewService, IUserManagementService userManagementService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceSettingsService workspaceSettingsService = workspaceSettingsService;
    private readonly IWorkspaceSettingsViewService workspaceSettingsViewService = workspaceSettingsViewService;
    private readonly IUserManagementService userManagementService = userManagementService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public IReadOnlyList<TicketStatus> Statuses { get; private set; } = [];
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = [];
    public IReadOnlyList<TicketType> Types { get; private set; } = [];

    public bool CanViewSettings { get; private set; }
    public bool CanEditSettings { get; private set; }
    public bool CanCreateSettings { get; private set; }
    public bool IsWorkspaceAdmin { get; private set; }
    public bool IsSystemAdmin { get; private set; }

    private async Task<(int userId, bool isAdmin)> ResolveUserAsync()
    {
        if (!this.TryGetUserId(out var uid))
        {
            return (0, false);
        }
        // isAdmin is computed via view service; return uid and false placeholder
        return (uid, false);
    }

    private async Task<bool> EnsurePermissionsAsync(int userId)
    {
        if (this.Workspace == null)
        {
            return false;
        }

        var user = await this.userManagementService.GetUserAsync(userId);
        this.IsSystemAdmin = user?.SystemAdmin == true;
        var data = await this.workspaceSettingsViewService.BuildAsync(this.Workspace.Id, userId);
        this.CanViewSettings = data.CanViewSettings;
        this.CanEditSettings = data.CanEditSettings;
        this.CanCreateSettings = data.CanCreateSettings;
        // populate lists and defaults
        this.Statuses = data.Statuses;
        this.Priorities = data.Priorities;
        this.Types = data.Types;
        return true;
    }

    /// <summary>
    /// Validates workspace existence and user permissions.
    /// </summary>
    /// <param name="slug">Workspace slug</param>
    /// <param name="permissionCheck">Permission check function</param>
    /// <returns>Error result if validation fails, null if validation succeeds (guarantees Workspace is not null)</returns>
    private async Task<IActionResult?> ValidateWorkspaceAndPermissionsAsync(string slug, Func<bool> permissionCheck)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var (uid, _) = await this.ResolveUserAsync();
        if (uid == 0)
        {
            return this.Forbid();
        }

        await this.EnsurePermissionsAsync(uid);
        if (!permissionCheck())
        {
            return this.Forbid();
        }

        // Returning null guarantees Workspace is not null
        return null;
    }

    [BindProperty]
    public string? NewStatusName { get; set; }
    [BindProperty]
    public string? NewStatusColor { get; set; }

    [BindProperty]
    public string? NewPriorityName { get; set; }
    [BindProperty]
    public string? NewPriorityColor { get; set; }

    [BindProperty]
    public string? NewTypeName { get; set; }
    [BindProperty]
    public string? NewTypeColor { get; set; }

    public async Task<IActionResult> OnPostAsync([FromRoute] string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var (uid, _) = await this.ResolveUserAsync();
        if (uid == 0)
        {
            return this.Forbid();
        }

        await this.EnsurePermissionsAsync(uid);
        if (this.EnsurePermissionOrForbid(this.CanEditSettings) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (!this.ModelState.IsValid)
        {
            this.SetErrorMessage("Please fix the validation errors.");
            await this.EnsurePermissionsAsync(uid);
            return this.Page();
        }
        var name = (this.Request.Form["Workspace.Name"].ToString() ?? this.Workspace.Name).Trim();
        var newSlug = (this.Request.Form["Workspace.Slug"].ToString() ?? this.Workspace.Slug).Trim();

        // Only allow system admins to change the slug
        if (newSlug != this.Workspace.Slug && !this.IsSystemAdmin)
        {
            this.TempData["ErrorMessage"] = "Only system administrators can change the workspace slug.";
            await this.EnsurePermissionsAsync(uid);
            return this.Page();
        }

        try
        {
            this.Workspace = await this.workspaceSettingsService.UpdateWorkspaceBasicSettingsAsync(this.Workspace.Id, name, newSlug);
        }
        catch (InvalidOperationException ex)
        {
            this.TempData["ErrorMessage"] = ex.Message;
            await this.EnsurePermissionsAsync(uid);
            return this.Page();
        }
        this.SetSuccessMessage("Workspace settings saved successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug = this.Workspace.Slug });
    }

    private async Task LoadDataAsync()
    {
        if (!this.TryGetUserId(out var uid))
        { this.Statuses = []; this.Priorities = []; this.Types = []; return; }
        await this.EnsurePermissionsAsync(uid);
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        var (uid, _) = await this.ResolveUserAsync();
        if (uid == 0)
        {
            return this.Forbid();
        }

        await this.EnsurePermissionsAsync(uid);
        if (this.EnsurePermissionOrForbid(this.CanViewSettings) is IActionResult permCheck)
        {
            return permCheck;
        }

        return this.Page();
    }

    public async Task<IActionResult> OnPostAddStatusAsync([FromRoute] string slug)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanCreateSettings) is IActionResult error)
        {
            return error;
        }

        var name = (this.NewStatusName ?? string.Empty).Trim();
        var color = (this.NewStatusColor ?? "neutral").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            this.SetErrorMessage("Status name is required.");
            return this.RedirectToPage("/Workspaces/Settings", new { slug });
        }
        try
        {
            await this.workspaceSettingsService.AddStatusAsync(this.Workspace!.Id, name, color, false);
            this.SetSuccessMessage($"Status '{name}' added successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.TempData["ErrorMessage"] = ex.Message;
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddPriorityAsync([FromRoute] string slug)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanCreateSettings) is IActionResult error)
        {
            return error;
        }

        var name = (this.NewPriorityName ?? string.Empty).Trim();
        var color = (this.NewPriorityColor ?? "neutral").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            this.SetErrorMessage("Priority name is required.");
            return this.RedirectToPage("/Workspaces/Settings", new { slug });
        }
        try
        {
            await this.workspaceSettingsService.AddPriorityAsync(this.Workspace!.Id, name, color);
            this.SetSuccessMessage($"Priority '{name}' added successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddTypeAsync([FromRoute] string slug)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanCreateSettings) is IActionResult error)
        {
            return error;
        }

        var name = (this.NewTypeName ?? string.Empty).Trim();
        var color = (this.NewTypeColor ?? "neutral").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            this.SetErrorMessage("Type name is required.");
            return this.RedirectToPage("/Workspaces/Settings", new { slug });
        }
        try
        {
            await this.workspaceSettingsService.AddTypeAsync(this.Workspace!.Id, name, color);
            this.SetSuccessMessage($"Type '{name}' added successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        var isClosedStateStr = this.Request.Form["isClosedState"];
        var isClosedState = !string.IsNullOrEmpty(isClosedStateStr) && (isClosedStateStr == "true" || isClosedStateStr == "on");
        try
        {
            var status = await this.workspaceSettingsService.UpdateStatusAsync(this.Workspace!.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder, isClosedState);
            this.SetSuccessMessage($"Status '{status.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        try
        {
            var priority = await this.workspaceSettingsService.UpdatePriorityAsync(this.Workspace!.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
            this.SetSuccessMessage($"Priority '{priority.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateTypeAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        try
        {
            var ticketType = await this.workspaceSettingsService.UpdateTypeAsync(this.Workspace!.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
            this.SetSuccessMessage($"Type '{ticketType.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteStatusAsync([FromRoute] string slug, [FromForm] int id)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        await this.workspaceSettingsService.DeleteStatusAsync(this.Workspace!.Id, id);
        this.SetSuccessMessage("Status deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeletePriorityAsync([FromRoute] string slug, [FromForm] int id)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        await this.workspaceSettingsService.DeletePriorityAsync(this.Workspace!.Id, id);
        this.SetSuccessMessage("Priority deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync([FromRoute] string slug, [FromForm] int id)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        await this.workspaceSettingsService.DeleteTypeAsync(this.Workspace!.Id, id);
        this.SetSuccessMessage("Type deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveNotificationSettingsAsync([FromRoute] string slug)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        this.TempData["NotificationSettingsSaved"] = true;
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveAllAsync([FromRoute] string slug)
    {
        if (await this.ValidateWorkspaceAndPermissionsAsync(slug, () => this.CanEditSettings) is IActionResult error)
        {
            return error;
        }

        try
        {
            var request = BulkSettingsFormParser.Parse(this.Request.Form);
            var result = await this.workspaceSettingsService.BulkUpdateSettingsAsync(this.Workspace!.Id, request);

            if (result.UpdatedWorkspace != null)
            {
                this.Workspace = result.UpdatedWorkspace;
            }

            if (result.Errors.Count > 0)
            {
                this.SetErrorMessage(result.Errors[0]);
                await this.LoadDataAsync();
                return this.Page();
            }

            this.SetSuccessMessage(result.ChangesApplied > 0
                ? $"Saved {result.ChangesApplied} change(s) successfully."
                : "Nothing to update.");

            return this.RedirectToPage("/Workspaces/Settings", new { slug = this.Workspace.Slug });
        }
        catch (Exception ex)
        {
            this.SetErrorMessage($"Error saving settings: {ex.Message}");
            await this.LoadDataAsync();
            return this.Page();
        }
    }
}



