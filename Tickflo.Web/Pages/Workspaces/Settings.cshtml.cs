using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class SettingsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePermRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    public SettingsModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, ITicketStatusRepository statusRepo, ITicketPriorityRepository priorityRepo, ITicketTypeRepository typeRepo, IRolePermissionRepository rolePermRepo)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
        _rolePermRepo = rolePermRepo;
    }

    public IReadOnlyList<Tickflo.Core.Entities.TicketStatus> Statuses { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketStatus>();
    public IReadOnlyList<Tickflo.Core.Entities.TicketPriority> Priorities { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketPriority>();
    public IReadOnlyList<Tickflo.Core.Entities.TicketType> Types { get; private set; } = Array.Empty<Tickflo.Core.Entities.TicketType>();

    public bool CanViewSettings { get; private set; }
    public bool CanEditSettings { get; private set; }
    public bool CanCreateSettings { get; private set; }
    public bool IsWorkspaceAdmin { get; private set; }

    private async Task<(int userId, bool isAdmin)> ResolveUserAsync()
    {
        if (!TryGetUserId(out var uid)) return (0, false);
        var isAdmin = Workspace != null && await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        return (uid, isAdmin);
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }

    private async Task<bool> EnsurePermissionsAsync(int userId, bool isAdmin)
    {
        IsWorkspaceAdmin = isAdmin;
        if (Workspace == null) return false;
        if (isAdmin)
        {
            CanViewSettings = CanEditSettings = CanCreateSettings = true;
            return true;
        }
        var perms = await _rolePermRepo.GetEffectivePermissionsForUserAsync(Workspace.Id, userId);
        if (!perms.TryGetValue("settings", out var eff)) eff = new EffectiveSectionPermission { Section = "settings" };
        CanViewSettings = eff.CanView;
        CanEditSettings = eff.CanEdit;
        CanCreateSettings = eff.CanCreate;
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

    // Notification configuration properties
    [BindProperty]
    public bool NotificationsEnabled { get; set; } = true;
    
    // Email Integration
    [BindProperty]
    public bool EmailIntegrationEnabled { get; set; } = true;
    [BindProperty]
    public string EmailProvider { get; set; } = "smtp";
    
    // SMS Integration
    [BindProperty]
    public bool SmsIntegrationEnabled { get; set; } = false;
    [BindProperty]
    public string SmsProvider { get; set; } = "none";
    
    // Push Integration
    [BindProperty]
    public bool PushIntegrationEnabled { get; set; } = false;
    [BindProperty]
    public string PushProvider { get; set; } = "none";
    
    // In-App (always enabled, no external integration needed)
    [BindProperty]
    public bool InAppNotificationsEnabled { get; set; } = true;
    
    // Scheduling
    [BindProperty]
    public int BatchNotificationDelay { get; set; } = 30;
    [BindProperty]
    public int DailySummaryHour { get; set; } = 9;
    [BindProperty]
    public bool MentionNotificationsUrgent { get; set; } = true;
    [BindProperty]
    public bool TicketAssignmentNotificationsHigh { get; set; } = true;

    public async Task<IActionResult> OnPostAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fix the validation errors.";
            await LoadDataAsync(Workspace);
            return Page();
        }
        
        // Update workspace
        Workspace.Name = (Request.Form["Workspace.Name"].ToString() ?? Workspace.Name).Trim();
        var newSlug = (Request.Form["Workspace.Slug"].ToString() ?? Workspace.Slug).Trim();
        
        // Check if slug changed and if new slug is unique
        if (newSlug != Workspace.Slug)
        {
            var existingWorkspace = await _workspaceRepo.FindBySlugAsync(newSlug);
            if (existingWorkspace != null)
            {
                TempData["ErrorMessage"] = "Slug is already in use. Please choose a different one.";
                await LoadDataAsync(Workspace);
                return Page();
            }
            Workspace.Slug = newSlug;
        }
        
        await _workspaceRepo.UpdateAsync(Workspace);
        TempData["SuccessMessage"] = "Workspace settings saved successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug = Workspace.Slug });
    }

    private async Task LoadDataAsync(Workspace workspace)
    {
        // Load or bootstrap default statuses
        var list = await _statusRepo.ListAsync(workspace.Id);
        if (list.Count == 0)
        {
            var defaults = new[]
            {
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = workspace.Id, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = workspace.Id, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = workspace.Id, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
            };
            foreach (var s in defaults)
            {
                await _statusRepo.CreateAsync(s);
            }
            list = await _statusRepo.ListAsync(workspace.Id);
        }
        Statuses = list;

        // Load or bootstrap default priorities
        var plist = await _priorityRepo.ListAsync(workspace.Id);
        if (plist.Count == 0)
        {
            var pdefs = new[]
            {
                new Tickflo.Core.Entities.TicketPriority { WorkspaceId = workspace.Id, Name = "Low", Color = "warning", SortOrder = 1 },
                new Tickflo.Core.Entities.TicketPriority { WorkspaceId = workspace.Id, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new Tickflo.Core.Entities.TicketPriority { WorkspaceId = workspace.Id, Name = "High", Color = "error", SortOrder = 3 },
            };
            foreach (var p in pdefs) { await _priorityRepo.CreateAsync(p); }
            plist = await _priorityRepo.ListAsync(workspace.Id);
        }
        Priorities = plist;
        // Load or bootstrap default types
        var tlist = await _typeRepo.ListAsync(workspace.Id);
        if (tlist.Count == 0)
        {
            var tdefs = new[]
            {
                new Tickflo.Core.Entities.TicketType { WorkspaceId = workspace.Id, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new Tickflo.Core.Entities.TicketType { WorkspaceId = workspace.Id, Name = "Bug", Color = "error", SortOrder = 2 },
                new Tickflo.Core.Entities.TicketType { WorkspaceId = workspace.Id, Name = "Feature", Color = "primary", SortOrder = 3 },
            };
            foreach (var t in tdefs) { await _typeRepo.CreateAsync(t); }
            tlist = await _typeRepo.ListAsync(workspace.Id);
        }
        Types = tlist;
        
        // Load notification integration settings from workspace metadata or use defaults
        NotificationsEnabled = true;
        EmailIntegrationEnabled = true;
        EmailProvider = "smtp";
        SmsIntegrationEnabled = false;
        SmsProvider = "none";
        PushIntegrationEnabled = false;
        PushProvider = "none";
        InAppNotificationsEnabled = true;
        BatchNotificationDelay = 30;
        DailySummaryHour = 9;
        MentionNotificationsUrgent = true;
        TicketAssignmentNotificationsHigh = true;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanViewSettings) return Forbid();
        await LoadDataAsync(Workspace);
        return Page();
    }

    public async Task<IActionResult> OnPostAddStatusAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanCreateSettings) return Forbid();
        var name = (NewStatusName ?? string.Empty).Trim();
        var color = (NewStatusColor ?? "neutral").Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var exists = await _statusRepo.FindByNameAsync(Workspace.Id, name);
            if (exists == null)
            {
                var maxOrder = (await _statusRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(s => s?.SortOrder ?? 0);
                await _statusRepo.CreateAsync(new Tickflo.Core.Entities.TicketStatus
                {
                    WorkspaceId = Workspace.Id,
                    Name = name,
                    Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color,
                    SortOrder = maxOrder + 1
                });
                TempData["SuccessMessage"] = $"Priority '{name}' added successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Priority '{name}' already exists.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Priority name is required.";
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddPriorityAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanCreateSettings) return Forbid();
        var name = (NewPriorityName ?? string.Empty).Trim();
        var color = (NewPriorityColor ?? "neutral").Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var exists = await _priorityRepo.FindAsync(Workspace.Id, name);
            if (exists == null)
            {
                var maxOrder = (await _priorityRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(p => p?.SortOrder ?? 0);
                await _priorityRepo.CreateAsync(new Tickflo.Core.Entities.TicketPriority
                {
                    WorkspaceId = Workspace.Id,
                    Name = name,
                    Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color,
                    SortOrder = maxOrder + 1
                });
                TempData["SuccessMessage"] = $"Priority '{name}' added successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Priority '{name}' already exists.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Priority name is required.";
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddTypeAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanCreateSettings) return Forbid();
        var name = (NewTypeName ?? string.Empty).Trim();
        var color = (NewTypeColor ?? "neutral").Trim();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var exists = await _typeRepo.FindByNameAsync(Workspace.Id, name);
            if (exists == null)
            {
                var maxOrder = (await _typeRepo.ListAsync(Workspace.Id)).DefaultIfEmpty().Max(t => t?.SortOrder ?? 0);
                await _typeRepo.CreateAsync(new Tickflo.Core.Entities.TicketType
                {
                    WorkspaceId = Workspace.Id,
                    Name = name,
                    Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color,
                    SortOrder = maxOrder + 1
                });
                TempData["SuccessMessage"] = $"Type '{name}' added successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Type '{name}' already exists.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Type name is required.";
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        var s = await _statusRepo.FindByIdAsync(Workspace.Id, id);
        if (s == null) return NotFound();
        s.Name = (name ?? s.Name).Trim();
        s.Color = string.IsNullOrWhiteSpace(color)
            ? (string.IsNullOrWhiteSpace(s.Color) ? "neutral" : s.Color)
            : color.Trim();
        s.SortOrder = sortOrder;
        // Read isClosedState from form
        var isClosedStateStr = Request.Form["isClosedState"];
        s.IsClosedState = !string.IsNullOrEmpty(isClosedStateStr) && (isClosedStateStr == "true" || isClosedStateStr == "on");
        await _statusRepo.UpdateAsync(s);
        TempData["SuccessMessage"] = $"Status '{s.Name}' updated successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        var p = await _priorityRepo.FindAsync(Workspace.Id, name) ?? (await _priorityRepo.ListAsync(Workspace.Id)).FirstOrDefault(x => x.Id == id);
        if (p == null) return NotFound();
        p.Name = (name ?? p.Name).Trim();
        p.Color = string.IsNullOrWhiteSpace(color)
            ? (string.IsNullOrWhiteSpace(p.Color) ? "neutral" : p.Color)
            : color.Trim();
        p.SortOrder = sortOrder;
        await _priorityRepo.UpdateAsync(p);
        TempData["SuccessMessage"] = $"Priority '{p.Name}' updated successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateTypeAsync([FromRoute] string slug, [FromForm] int id, [FromForm] string name, [FromForm] string color, [FromForm] int sortOrder)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        var t = await _typeRepo.FindByIdAsync(Workspace.Id, id);
        if (t == null) return NotFound();
        t.Name = (name ?? t.Name).Trim();
        t.Color = string.IsNullOrWhiteSpace(color)
            ? (string.IsNullOrWhiteSpace(t.Color) ? "neutral" : t.Color)
            : color.Trim();
        t.SortOrder = sortOrder;
        await _typeRepo.UpdateAsync(t);
        TempData["SuccessMessage"] = $"Type '{t.Name}' updated successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteStatusAsync([FromRoute] string slug, [FromForm] int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        await _statusRepo.DeleteAsync(Workspace.Id, id);
        TempData["SuccessMessage"] = "Status deleted successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeletePriorityAsync([FromRoute] string slug, [FromForm] int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        await _priorityRepo.DeleteAsync(Workspace.Id, id);
        TempData["SuccessMessage"] = "Priority deleted successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync([FromRoute] string slug, [FromForm] int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        await _typeRepo.DeleteAsync(Workspace.Id, id);
        TempData["SuccessMessage"] = "Type deleted successfully!";
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveNotificationSettingsAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        
        // TODO: Save notification settings to workspace_settings table or workspace metadata
        // For now, this would just redirect back
        // In production, you'd store these in a workspace_settings table:
        // await _workspaceSettingsRepo.SaveAsync(new WorkspaceSettings
        // {
        //     WorkspaceId = Workspace.Id,
        //     NotificationsEnabled = NotificationsEnabled,
        //     EmailNotificationsEnabled = EmailNotificationsEnabled,
        //     ...
        // });
        
        TempData["NotificationSettingsSaved"] = true;
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveAllAsync([FromRoute] string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();

        int changedCount = 0;

        try
        {
            var form = Request.Form;

            // Workspace identity
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
                        TempData["ErrorMessage"] = "Slug is already in use. Please choose a different one.";
                        await LoadDataAsync(Workspace);
                        return Page();
                    }
                    Workspace.Slug = newSlug;
                }
            }

            await _workspaceRepo.UpdateAsync(Workspace);
            changedCount++;

            // Ticket statuses (update/delete)
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

            // New status
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
                    TempData["ErrorMessage"] = $"Status '{newStatusName}' already exists.";
                }
            }

            // Ticket priorities (update/delete)
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

            // New priority
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
                    TempData["ErrorMessage"] = $"Priority '{newPriorityName}' already exists.";
                }
            }

            // Ticket types (update/delete)
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

            // New type
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
                    TempData["ErrorMessage"] = $"Type '{newTypeName}' already exists.";
                }
            }

            // Notification settings
            NotificationsEnabled = form["NotificationsEnabled"] == "true" || form["NotificationsEnabled"] == "on";
            EmailIntegrationEnabled = form["EmailIntegrationEnabled"] == "true" || form["EmailIntegrationEnabled"] == "on";
            SmsIntegrationEnabled = form["SmsIntegrationEnabled"] == "true" || form["SmsIntegrationEnabled"] == "on";
            PushIntegrationEnabled = form["PushIntegrationEnabled"] == "true" || form["PushIntegrationEnabled"] == "on";
            InAppNotificationsEnabled = form["InAppNotificationsEnabled"] == "true" || form["InAppNotificationsEnabled"] == "on";
            if (int.TryParse(form["BatchNotificationDelay"], out var batchDelay)) BatchNotificationDelay = batchDelay;
            if (int.TryParse(form["DailySummaryHour"], out var summaryHour)) DailySummaryHour = summaryHour;
            MentionNotificationsUrgent = form["MentionNotificationsUrgent"] == "true" || form["MentionNotificationsUrgent"] == "on";
            TicketAssignmentNotificationsHigh = form["TicketAssignmentNotificationsHigh"] == "true" || form["TicketAssignmentNotificationsHigh"] == "on";
            changedCount++;

            TempData["SuccessMessage"] = changedCount > 0
                ? $"Saved {changedCount} change(s) successfully."
                : "Nothing to update.";

            return RedirectToPage("/Workspaces/Settings", new { slug = Workspace.Slug });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error saving settings: {ex.Message}";
            await LoadDataAsync(Workspace);
            return Page();
        }
    }
}


