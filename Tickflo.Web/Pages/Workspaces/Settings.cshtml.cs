namespace Tickflo.Web.Pages.Workspaces;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Users;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

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
        if (this.EnsurePermissionOrForbid(this.CanCreateSettings) is IActionResult permCheck)
        {
            return permCheck;
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
            await this.workspaceSettingsService.AddStatusAsync(this.Workspace.Id, name, color, false);
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
        if (!this.CanCreateSettings)
        {
            return this.Forbid();
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
            await this.workspaceSettingsService.AddPriorityAsync(this.Workspace.Id, name, color);
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
        if (!this.CanCreateSettings)
        {
            return this.Forbid();
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
            await this.workspaceSettingsService.AddTypeAsync(this.Workspace.Id, name, color);
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

        var isClosedStateStr = this.Request.Form["isClosedState"];
        var isClosedState = !string.IsNullOrEmpty(isClosedStateStr) && (isClosedStateStr == "true" || isClosedStateStr == "on");
        try
        {
            var s = await this.workspaceSettingsService.UpdateStatusAsync(this.Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder, isClosedState);
            this.SetSuccessMessage($"Status '{s.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
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

        try
        {
            var p = await this.workspaceSettingsService.UpdatePriorityAsync(this.Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
            this.SetSuccessMessage($"Priority '{p.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateTypeAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
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

        try
        {
            var t = await this.workspaceSettingsService.UpdateTypeAsync(this.Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
            this.SetSuccessMessage($"Type '{t.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
        }
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteStatusAsync([FromRoute] string slug, [FromForm] int id)
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

        await this.workspaceSettingsService.DeleteStatusAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Status deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeletePriorityAsync([FromRoute] string slug, [FromForm] int id)
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

        await this.workspaceSettingsService.DeletePriorityAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Priority deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync([FromRoute] string slug, [FromForm] int id)
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

        await this.workspaceSettingsService.DeleteTypeAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Type deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveNotificationSettingsAsync([FromRoute] string slug)
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

        this.TempData["NotificationSettingsSaved"] = true;
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveAllAsync([FromRoute] string slug)
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

        var changedCount = 0;

        try
        {
            var form = this.Request.Form;

            var workspaceName = form["Workspace.Name"].ToString();
            var workspaceSlug = form["Workspace.Slug"].ToString();

            // Update workspace basic settings if provided
            if (!string.IsNullOrWhiteSpace(workspaceName) || !string.IsNullOrWhiteSpace(workspaceSlug))
            {
                var name = !string.IsNullOrWhiteSpace(workspaceName) ? workspaceName.Trim() : this.Workspace.Name;
                var newSlug = !string.IsNullOrWhiteSpace(workspaceSlug) ? workspaceSlug.Trim() : this.Workspace.Slug;
                
                try
                {
                    this.Workspace = await this.workspaceSettingsService.UpdateWorkspaceBasicSettingsAsync(this.Workspace.Id, name, newSlug);
                    changedCount++;
                }
                catch (InvalidOperationException ex)
                {
                    this.SetErrorMessage(ex.Message);
                    await this.LoadDataAsync();
                    return this.Page();
                }
            }

            var statusMatches = form.Keys
                .Select(k => MyRegex().Match(k))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            // Get current status list to validate IDs
            var statusList = this.Statuses;
            if (statusList.Count == 0)
            {
                var viewData = await this.workspaceSettingsViewService.BuildAsync(this.Workspace.Id, uid);
                statusList = viewData.Statuses;
            }

            foreach (var group in statusMatches)
            {
                var statusId = group.Key;
                var status = statusList.FirstOrDefault(s => s.Id == statusId);
                if (status == null)
                {
                    continue;
                }

                var deleteFlag = form[$"statuses[{statusId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    try
                    {
                        await this.workspaceSettingsService.DeleteStatusAsync(this.Workspace.Id, statusId);
                        changedCount++;
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore deletion errors
                    }
                    continue;
                }

                var name = form[$"statuses[{statusId}].name"].ToString();
                var color = form[$"statuses[{statusId}].color"].ToString();
                var order = form[$"statuses[{statusId}].sortOrder"].ToString();
                var closed = form[$"statuses[{statusId}].isClosedState"].ToString();

                var statusName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : status.Name;
                var statusColor = !string.IsNullOrWhiteSpace(color) ? color.Trim() : (string.IsNullOrWhiteSpace(status.Color) ? "neutral" : status.Color);
                var statusSortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : status.SortOrder;
                var isClosedState = closed is "true" or "on";

                try
                {
                    await this.workspaceSettingsService.UpdateStatusAsync(this.Workspace.Id, statusId, statusName, statusColor, statusSortOrder, isClosedState);
                    changedCount++;
                }
                catch (InvalidOperationException)
                {
                    // Ignore update errors
                }
            }

            var newStatusName = (form["NewStatusName"].ToString() ?? string.Empty).Trim();
            var newStatusColor = (form["NewStatusColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newStatusName))
            {
                try
                {
                    await this.workspaceSettingsService.AddStatusAsync(this.Workspace.Id, newStatusName, newStatusColor, false);
                    changedCount++;
                }
                catch (InvalidOperationException ex)
                {
                    this.SetErrorMessage(ex.Message);
                }
            }

            var priorityMatches = form.Keys
                .Select(k => MyRegex1().Match(k))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            // Get current priority list to validate IDs
            var priorityList = this.Priorities;
            if (priorityList.Count == 0)
            {
                var viewData = await this.workspaceSettingsViewService.BuildAsync(this.Workspace.Id, uid);
                priorityList = viewData.Priorities;
            }

            foreach (var group in priorityMatches)
            {
                var priorityId = group.Key;
                var priority = priorityList.FirstOrDefault(x => x.Id == priorityId);
                if (priority == null)
                {
                    continue;
                }

                var deleteFlag = form[$"priorities[{priorityId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    try
                    {
                        await this.workspaceSettingsService.DeletePriorityAsync(this.Workspace.Id, priorityId);
                        changedCount++;
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore deletion errors
                    }
                    continue;
                }

                var name = form[$"priorities[{priorityId}].name"].ToString();
                var color = form[$"priorities[{priorityId}].color"].ToString();
                var order = form[$"priorities[{priorityId}].sortOrder"].ToString();

                var priorityName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : priority.Name;
                var priorityColor = !string.IsNullOrWhiteSpace(color) ? color.Trim() : (string.IsNullOrWhiteSpace(priority.Color) ? "neutral" : priority.Color);
                var prioritySortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : priority.SortOrder;

                try
                {
                    await this.workspaceSettingsService.UpdatePriorityAsync(this.Workspace.Id, priorityId, priorityName, priorityColor, prioritySortOrder);
                    changedCount++;
                }
                catch (InvalidOperationException)
                {
                    // Ignore update errors
                }
            }

            var newPriorityName = (form["NewPriorityName"].ToString() ?? string.Empty).Trim();
            var newPriorityColor = (form["NewPriorityColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newPriorityName))
            {
                try
                {
                    await this.workspaceSettingsService.AddPriorityAsync(this.Workspace.Id, newPriorityName, newPriorityColor);
                    changedCount++;
                }
                catch (InvalidOperationException ex)
                {
                    this.SetErrorMessage(ex.Message);
                }
            }

            var typeMatches = form.Keys
                .Select(k => MyRegex2().Match(k))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            // Get current type list to validate IDs
            var typeList = this.Types;
            if (typeList.Count == 0)
            {
                var viewData = await this.workspaceSettingsViewService.BuildAsync(this.Workspace.Id, uid);
                typeList = viewData.Types;
            }

            foreach (var group in typeMatches)
            {
                var typeId = group.Key;
                var type = typeList.FirstOrDefault(t => t.Id == typeId);
                if (type == null)
                {
                    continue;
                }

                var deleteFlag = form[$"types[{typeId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    try
                    {
                        await this.workspaceSettingsService.DeleteTypeAsync(this.Workspace.Id, typeId);
                        changedCount++;
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore deletion errors
                    }
                    continue;
                }

                var name = form[$"types[{typeId}].name"].ToString();
                var color = form[$"types[{typeId}].color"].ToString();
                var order = form[$"types[{typeId}].sortOrder"].ToString();

                var typeName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : type.Name;
                var typeColor = !string.IsNullOrWhiteSpace(color) ? color.Trim() : (string.IsNullOrWhiteSpace(type.Color) ? "neutral" : type.Color);
                var typeSortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : type.SortOrder;

                try
                {
                    await this.workspaceSettingsService.UpdateTypeAsync(this.Workspace.Id, typeId, typeName, typeColor, typeSortOrder);
                    changedCount++;
                }
                catch (InvalidOperationException)
                {
                    // Ignore update errors
                }
            }

            var newTypeName = (form["NewTypeName"].ToString() ?? string.Empty).Trim();
            var newTypeColor = (form["NewTypeColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newTypeName))
            {
                try
                {
                    await this.workspaceSettingsService.AddTypeAsync(this.Workspace.Id, newTypeName, newTypeColor);
                    changedCount++;
                }
                catch (InvalidOperationException ex)
                {
                    this.SetErrorMessage(ex.Message);
                }
            }

            this.SetSuccessMessage(changedCount > 0
                ? $"Saved {changedCount} change(s) successfully."
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

    [GeneratedRegex(@"^statuses\[(\d+)\]\.(.+)$")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"^priorities\[(\d+)\]\.(.+)$")]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"^types\[(\d+)\]\.(.+)$")]
    private static partial Regex MyRegex2();
}




