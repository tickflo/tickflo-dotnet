namespace Tickflo.Web.Pages.Workspaces;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public partial class SettingsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepository, ITicketStatusRepository statusRepository, ITicketPriorityRepository priorityRepository, ITicketTypeRepository ticketTypeRepository, IWorkspaceSettingsService settingsService, IWorkspaceSettingsViewService settingsViewService, IUserRepository userRepository) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly ITicketStatusRepository statusRepository = statusRepository;
    private readonly ITicketPriorityRepository priorityRepository = priorityRepository;
    private readonly ITicketTypeRepository ticketTypeRepository = ticketTypeRepository;
    private readonly IWorkspaceSettingsService _settingsService = settingsService;
    private readonly IWorkspaceSettingsViewService _settingsViewService = settingsViewService;
    private readonly IUserRepository userRepository = userRepository;
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

        var user = await this.userRepository.FindByIdAsync(userId);
        this.IsSystemAdmin = user?.SystemAdmin == true;
        var data = await this._settingsViewService.BuildAsync(this.Workspace.Id, userId);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            this.Workspace = await this._settingsService.UpdateWorkspaceBasicSettingsAsync(this.Workspace.Id, name, newSlug);
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

    private async Task LoadDataAsync(Workspace workspace)
    {
        if (!this.TryGetUserId(out var uid))
        { this.Statuses = []; this.Priorities = []; this.Types = []; return; }
        await this.EnsurePermissionsAsync(uid);
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            await this._settingsService.AddStatusAsync(this.Workspace.Id, name, color, false);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            await this._settingsService.AddPriorityAsync(this.Workspace.Id, name, color);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            await this._settingsService.AddTypeAsync(this.Workspace.Id, name, color);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            var s = await this._settingsService.UpdateStatusAsync(this.Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder, isClosedState);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            var p = await this._settingsService.UpdatePriorityAsync(this.Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
            var t = await this._settingsService.UpdateTypeAsync(this.Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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

        await this._settingsService.DeleteStatusAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Status deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeletePriorityAsync([FromRoute] string slug, [FromForm] int id)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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

        await this._settingsService.DeletePriorityAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Priority deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync([FromRoute] string slug, [FromForm] int id)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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

        await this._settingsService.DeleteTypeAsync(this.Workspace.Id, id);
        this.SetSuccessMessage("Type deleted successfully!");
        return this.RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveNotificationSettingsAsync([FromRoute] string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
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

            if (!string.IsNullOrWhiteSpace(workspaceName))
            {
                this.Workspace.Name = workspaceName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(workspaceSlug))
            {
                var newSlug = workspaceSlug.Trim();
                if (newSlug != this.Workspace.Slug)
                {
                    var existing = await this.workspaceRepository.FindBySlugAsync(newSlug);
                    if (existing != null)
                    {
                        this.SetErrorMessage("Slug is already in use. Please choose a different one.");
                        await this.LoadDataAsync(this.Workspace);
                        return this.Page();
                    }
                    this.Workspace.Slug = newSlug;
                }
            }

            await this.workspaceRepository.UpdateAsync(this.Workspace);
            changedCount++;

            var statusMatches = form.Keys
                .Select(k => MyRegex().Match(k))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            foreach (var group in statusMatches)
            {
                var statusId = group.Key;
                var status = await this.statusRepository.FindByIdAsync(this.Workspace.Id, statusId);
                if (status == null)
                {
                    continue;
                }

                var deleteFlag = form[$"statuses[{statusId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    await this.statusRepository.DeleteAsync(this.Workspace.Id, statusId);
                    changedCount++;
                    continue;
                }

                var name = form[$"statuses[{statusId}].name"].ToString();
                var color = form[$"statuses[{statusId}].color"].ToString();
                var order = form[$"statuses[{statusId}].sortOrder"].ToString();
                var closed = form[$"statuses[{statusId}].isClosedState"].ToString();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    status.Name = name.Trim();
                }

                status.Color = string.IsNullOrWhiteSpace(color)
                    ? (string.IsNullOrWhiteSpace(status.Color) ? "neutral" : status.Color)
                    : color.Trim();

                if (int.TryParse(order, out var sortOrder))
                {
                    status.SortOrder = sortOrder;
                }

                status.IsClosedState = closed is "true" or "on";
                await this.statusRepository.UpdateAsync(status);
                changedCount++;
            }

            var newStatusName = (form["NewStatusName"].ToString() ?? string.Empty).Trim();
            var newStatusColor = (form["NewStatusColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newStatusName))
            {
                var exists = await this.statusRepository.FindByNameAsync(this.Workspace.Id, newStatusName);
                if (exists == null)
                {
                    var maxOrder = (await this.statusRepository.ListAsync(this.Workspace.Id)).DefaultIfEmpty().Max(s => s?.SortOrder ?? 0);
                    await this.statusRepository.CreateAsync(new TicketStatus
                    {
                        WorkspaceId = this.Workspace.Id,
                        Name = newStatusName,
                        Color = string.IsNullOrWhiteSpace(newStatusColor) ? "neutral" : newStatusColor,
                        SortOrder = maxOrder + 1
                    });
                    changedCount++;
                }
                else
                {
                    this.SetErrorMessage($"Status '{newStatusName}' already exists.");
                }
            }

            var priorityMatches = form.Keys
                .Select(k => MyRegex1().Match(k))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            var priorityList = await this.priorityRepository.ListAsync(this.Workspace.Id);

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
                    await this.priorityRepository.DeleteAsync(this.Workspace.Id, priorityId);
                    changedCount++;
                    continue;
                }

                var name = form[$"priorities[{priorityId}].name"].ToString();
                var color = form[$"priorities[{priorityId}].color"].ToString();
                var order = form[$"priorities[{priorityId}].sortOrder"].ToString();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    priority.Name = name.Trim();
                }

                priority.Color = string.IsNullOrWhiteSpace(color)
                    ? (string.IsNullOrWhiteSpace(priority.Color) ? "neutral" : priority.Color)
                    : color.Trim();

                if (int.TryParse(order, out var sortOrder))
                {
                    priority.SortOrder = sortOrder;
                }

                await this.priorityRepository.UpdateAsync(priority);
                changedCount++;
            }

            var newPriorityName = (form["NewPriorityName"].ToString() ?? string.Empty).Trim();
            var newPriorityColor = (form["NewPriorityColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newPriorityName))
            {
                var exists = await this.priorityRepository.FindAsync(this.Workspace.Id, newPriorityName);
                if (exists == null)
                {
                    var maxOrder = (await this.priorityRepository.ListAsync(this.Workspace.Id)).DefaultIfEmpty().Max(p => p?.SortOrder ?? 0);
                    await this.priorityRepository.CreateAsync(new TicketPriority
                    {
                        WorkspaceId = this.Workspace.Id,
                        Name = newPriorityName,
                        Color = string.IsNullOrWhiteSpace(newPriorityColor) ? "neutral" : newPriorityColor,
                        SortOrder = maxOrder + 1
                    });
                    changedCount++;
                }
                else
                {
                    this.SetErrorMessage($"Priority '{newPriorityName}' already exists.");
                }
            }

            var typeMatches = form.Keys
                .Select(k => MyRegex2().Match(k))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            foreach (var group in typeMatches)
            {
                var typeId = group.Key;
                var type = await this.ticketTypeRepository.FindByIdAsync(this.Workspace.Id, typeId);
                if (type == null)
                {
                    continue;
                }

                var deleteFlag = form[$"types[{typeId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    await this.ticketTypeRepository.DeleteAsync(this.Workspace.Id, typeId);
                    changedCount++;
                    continue;
                }

                var name = form[$"types[{typeId}].name"].ToString();
                var color = form[$"types[{typeId}].color"].ToString();
                var order = form[$"types[{typeId}].sortOrder"].ToString();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    type.Name = name.Trim();
                }

                type.Color = string.IsNullOrWhiteSpace(color)
                    ? (string.IsNullOrWhiteSpace(type.Color) ? "neutral" : type.Color)
                    : color.Trim();

                if (int.TryParse(order, out var sortOrder))
                {
                    type.SortOrder = sortOrder;
                }

                await this.ticketTypeRepository.UpdateAsync(type);
                changedCount++;
            }

            var newTypeName = (form["NewTypeName"].ToString() ?? string.Empty).Trim();
            var newTypeColor = (form["NewTypeColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newTypeName))
            {
                var exists = await this.ticketTypeRepository.FindByNameAsync(this.Workspace.Id, newTypeName);
                if (exists == null)
                {
                    var maxOrder = (await this.ticketTypeRepository.ListAsync(this.Workspace.Id)).DefaultIfEmpty().Max(t => t?.SortOrder ?? 0);
                    await this.ticketTypeRepository.CreateAsync(new TicketType
                    {
                        WorkspaceId = this.Workspace.Id,
                        Name = newTypeName,
                        Color = string.IsNullOrWhiteSpace(newTypeColor) ? "neutral" : newTypeColor,
                        SortOrder = maxOrder + 1
                    });
                    changedCount++;
                }
                else
                {
                    this.SetErrorMessage($"Type '{newTypeName}' already exists.");
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
            await this.LoadDataAsync(this.Workspace);
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




