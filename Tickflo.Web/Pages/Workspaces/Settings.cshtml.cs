using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Workspace;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class SettingsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    private readonly IWorkspaceSettingsService _settingsService;
    private readonly IWorkspaceSettingsViewService _settingsViewService;
    private readonly IUserRepository _userRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public SettingsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, ITicketStatusRepository statusRepo, ITicketPriorityRepository priorityRepo, ITicketTypeRepository typeRepo, IWorkspaceSettingsService settingsService, IWorkspaceSettingsViewService settingsViewService, IUserRepository userRepo)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _settingsService = settingsService;
        _settingsViewService = settingsViewService;
        _userRepo = userRepo;
    }

    public IReadOnlyList<Tickflo.Core.Entities.TicketStatus> Statuses { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketStatus>();
    public IReadOnlyList<Tickflo.Core.Entities.TicketPriority> Priorities { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketPriority>();
    public IReadOnlyList<Tickflo.Core.Entities.TicketType> Types { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketType>();

    public bool CanViewSettings { get; private set; }
    public bool CanEditSettings { get; private set; }
    public bool CanCreateSettings { get; private set; }
    public bool IsWorkspaceAdmin { get; private set; }
    public bool IsSystemAdmin { get; private set; }

    private async Task<(int userId, bool isAdmin)> ResolveUserAsync()
    {
        if (!TryGetUserId(out var uid)) return (0, false);
        // isAdmin is computed via view service; return uid and false placeholder
        return (uid, false);
    }

    private async Task<bool> EnsurePermissionsAsync(int userId)
    {
        if (Workspace == null) return false;
        var user = await _userRepo.FindByIdAsync(userId);
        IsSystemAdmin = user?.SystemAdmin == true;
        var data = await _settingsViewService.BuildAsync(Workspace.Id, userId);
        CanViewSettings = data.CanViewSettings;
        CanEditSettings = data.CanEditSettings;
        CanCreateSettings = data.CanCreateSettings;
        // populate lists and defaults
        Statuses = data.Statuses;
        Priorities = data.Priorities;
        Types = data.Types;
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
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        
        if (!ModelState.IsValid)
        {
            SetErrorMessage("Please fix the validation errors.");
            await EnsurePermissionsAsync(uid);
            return Page();
        }
        var name = (Request.Form["Workspace.Name"].ToString() ?? Workspace.Name).Trim();
        var newSlug = (Request.Form["Workspace.Slug"].ToString() ?? Workspace.Slug).Trim();
        
        // Only allow system admins to change the slug
        if (newSlug != Workspace.Slug && !IsSystemAdmin)
        {
            TempData["ErrorMessage"] = "Only system administrators can change the workspace slug.";
            await EnsurePermissionsAsync(uid);
            return Page();
        }
        
        try
        {
            Workspace = await _settingsService.UpdateWorkspaceBasicSettingsAsync(Workspace.Id, name, newSlug);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            await EnsurePermissionsAsync(uid);
            return Page();
        }
        SetSuccessMessage("Workspace settings saved successfully!");
        return RedirectToPage("/Workspaces/Settings", new { slug = Workspace.Slug });
    }

    private async Task LoadDataAsync(Workspace workspace)
    {
        if (!TryGetUserId(out var uid)) { Statuses = Array.Empty<Tickflo.Core.Entities.TicketStatus>(); Priorities = Array.Empty<Tickflo.Core.Entities.TicketPriority>(); Types = Array.Empty<Tickflo.Core.Entities.TicketType>(); return; }
        await EnsurePermissionsAsync(uid);
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanViewSettings) is IActionResult permCheck) return permCheck;
        return Page();
    }

    public async Task<IActionResult> OnPostAddStatusAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanCreateSettings) is IActionResult permCheck) return permCheck;
        var name = (NewStatusName ?? string.Empty).Trim();
        var color = (NewStatusColor ?? "neutral").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetErrorMessage("Status name is required.");
            return RedirectToPage("/Workspaces/Settings", new { slug });
        }
        try
        {
            await _settingsService.AddStatusAsync(Workspace.Id, name, color, false);
            SetSuccessMessage($"Status '{name}' added successfully!");
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddPriorityAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (!CanCreateSettings) return Forbid();
        var name = (NewPriorityName ?? string.Empty).Trim();
        var color = (NewPriorityColor ?? "neutral").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetErrorMessage("Priority name is required.");
            return RedirectToPage("/Workspaces/Settings", new { slug });
        }
        try
        {
            await _settingsService.AddPriorityAsync(Workspace.Id, name, color);
            SetSuccessMessage($"Priority '{name}' added successfully!");
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddTypeAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (!CanCreateSettings) return Forbid();
        var name = (NewTypeName ?? string.Empty).Trim();
        var color = (NewTypeColor ?? "neutral").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            SetErrorMessage("Type name is required.");
            return RedirectToPage("/Workspaces/Settings", new { slug });
        }
        try
        {
            await _settingsService.AddTypeAsync(Workspace.Id, name, color);
            SetSuccessMessage($"Type '{name}' added successfully!");
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        var isClosedStateStr = Request.Form["isClosedState"];
        var isClosedState = !string.IsNullOrEmpty(isClosedStateStr) && (isClosedStateStr == "true" || isClosedStateStr == "on");
        try
        {
            var s = await _settingsService.UpdateStatusAsync(Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder, isClosedState);
            SetSuccessMessage($"Status '{s.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        try
        {
            var p = await _settingsService.UpdatePriorityAsync(Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
            SetSuccessMessage($"Priority '{p.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateTypeAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        try
        {
            var t = await _settingsService.UpdateTypeAsync(Workspace.Id, id, name?.Trim() ?? string.Empty, string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim(), sortOrder);
            SetSuccessMessage($"Type '{t.Name}' updated successfully!");
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteStatusAsync([FromRoute] string slug, [FromForm] int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        await _settingsService.DeleteStatusAsync(Workspace.Id, id);
        SetSuccessMessage("Status deleted successfully!");
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeletePriorityAsync([FromRoute] string slug, [FromForm] int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        await _settingsService.DeletePriorityAsync(Workspace.Id, id);
        SetSuccessMessage("Priority deleted successfully!");
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync([FromRoute] string slug, [FromForm] int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        await _settingsService.DeleteTypeAsync(Workspace.Id, id);
        SetSuccessMessage("Type deleted successfully!");
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveNotificationSettingsAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;
        
        TempData["NotificationSettingsSaved"] = true;
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveAllAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, _) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid);
        if (EnsurePermissionOrForbid(CanEditSettings) is IActionResult permCheck) return permCheck;

        int changedCount = 0;

        try
        {
            var form = Request.Form;

            var workspaceName = form["Workspace.Name"].ToString();
            var workspaceSlug = form["Workspace.Slug"].ToString();

            if (!string.IsNullOrWhiteSpace(workspaceName))
            {
                Workspace.Name = workspaceName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(workspaceSlug))
            {
                var newSlug = workspaceSlug.Trim();
                if (newSlug != Workspace.Slug)
                {
                    var existing = await _workspaceRepo.FindBySlugAsync(newSlug);
                    if (existing != null)
                    {
                        SetErrorMessage("Slug is already in use. Please choose a different one.");
                        await LoadDataAsync(Workspace);
                        return Page();
                    }
                    Workspace.Slug = newSlug;
                }
            }

            await _workspaceRepo.UpdateAsync(Workspace);
            changedCount++;

            var statusMatches = form.Keys
                .Select(k => Regex.Match(k, @"^statuses\[(\d+)\]\.(.+)$"))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            foreach (var group in statusMatches)
            {
                var statusId = group.Key;
                var status = await _statusRepo.FindByIdAsync(Workspace.Id, statusId);
                if (status == null) continue;

                var deleteFlag = form[$"statuses[{statusId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    await _statusRepo.DeleteAsync(Workspace.Id, statusId);
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

                if (int.TryParse(order, out int sortOrder))
                {
                    status.SortOrder = sortOrder;
                }

                status.IsClosedState = closed == "true" || closed == "on";
                await _statusRepo.UpdateAsync(status);
                changedCount++;
            }

            var newStatusName = (form["NewStatusName"].ToString() ?? string.Empty).Trim();
            var newStatusColor = (form["NewStatusColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newStatusName))
            {
                var exists = await _statusRepo.FindByNameAsync(Workspace.Id, newStatusName);
                if (exists == null)
                {
                    var maxOrder = (await _statusRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(s => s?.SortOrder ?? 0);
                    await _statusRepo.CreateAsync(new Tickflo.Core.Entities.TicketStatus
                    {
                        WorkspaceId = Workspace.Id,
                        Name = newStatusName,
                        Color = string.IsNullOrWhiteSpace(newStatusColor) ? "neutral" : newStatusColor,
                        SortOrder = maxOrder + 1
                    });
                    changedCount++;
                }
                else
                {
                    SetErrorMessage($"Status '{newStatusName}' already exists.");
                }
            }

            var priorityMatches = form.Keys
                .Select(k => Regex.Match(k, @"^priorities\[(\d+)\]\.(.+)$"))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            var priorityList = await _priorityRepo.ListAsync(Workspace.Id);

            foreach (var group in priorityMatches)
            {
                var priorityId = group.Key;
                var priority = priorityList.FirstOrDefault(x => x.Id == priorityId);
                if (priority == null) continue;

                var deleteFlag = form[$"priorities[{priorityId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    await _priorityRepo.DeleteAsync(Workspace.Id, priorityId);
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

                if (int.TryParse(order, out int sortOrder))
                {
                    priority.SortOrder = sortOrder;
                }

                await _priorityRepo.UpdateAsync(priority);
                changedCount++;
            }

            var newPriorityName = (form["NewPriorityName"].ToString() ?? string.Empty).Trim();
            var newPriorityColor = (form["NewPriorityColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newPriorityName))
            {
                var exists = await _priorityRepo.FindAsync(Workspace.Id, newPriorityName);
                if (exists == null)
                {
                    var maxOrder = (await _priorityRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(p => p?.SortOrder ?? 0);
                    await _priorityRepo.CreateAsync(new Tickflo.Core.Entities.TicketPriority
                    {
                        WorkspaceId = Workspace.Id,
                        Name = newPriorityName,
                        Color = string.IsNullOrWhiteSpace(newPriorityColor) ? "neutral" : newPriorityColor,
                        SortOrder = maxOrder + 1
                    });
                    changedCount++;
                }
                else
                {
                    SetErrorMessage($"Priority '{newPriorityName}' already exists.");
                }
            }

            var typeMatches = form.Keys
                .Select(k => Regex.Match(k, @"^types\[(\d+)\]\.(.+)$"))
                .Where(m => m.Success)
                .GroupBy(m => int.Parse(m.Groups[1].Value));

            foreach (var group in typeMatches)
            {
                var typeId = group.Key;
                var type = await _typeRepo.FindByIdAsync(Workspace.Id, typeId);
                if (type == null) continue;

                var deleteFlag = form[$"types[{typeId}].delete"].ToString();
                if (!string.IsNullOrEmpty(deleteFlag))
                {
                    await _typeRepo.DeleteAsync(Workspace.Id, typeId);
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

                if (int.TryParse(order, out int sortOrder))
                {
                    type.SortOrder = sortOrder;
                }

                await _typeRepo.UpdateAsync(type);
                changedCount++;
            }

            var newTypeName = (form["NewTypeName"].ToString() ?? string.Empty).Trim();
            var newTypeColor = (form["NewTypeColor"].ToString() ?? "neutral").Trim();
            if (!string.IsNullOrWhiteSpace(newTypeName))
            {
                var exists = await _typeRepo.FindByNameAsync(Workspace.Id, newTypeName);
                if (exists == null)
                {
                    var maxOrder = (await _typeRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(t => t?.SortOrder ?? 0);
                    await _typeRepo.CreateAsync(new Tickflo.Core.Entities.TicketType
                    {
                        WorkspaceId = Workspace.Id,
                        Name = newTypeName,
                        Color = string.IsNullOrWhiteSpace(newTypeColor) ? "neutral" : newTypeColor,
                        SortOrder = maxOrder + 1
                    });
                    changedCount++;
                }
                else
                {
                    SetErrorMessage($"Type '{newTypeName}' already exists.");
                }
            }

            SetSuccessMessage(changedCount > 0
                ? $"Saved {changedCount} change(s) successfully."
                : "Nothing to update.");

            return RedirectToPage("/Workspaces/Settings", new { slug = Workspace.Slug });
        }
        catch (Exception ex)
        {
            SetErrorMessage($"Error saving settings: {ex.Message}");
            await LoadDataAsync(Workspace);
            return Page();
        }
    }
}




