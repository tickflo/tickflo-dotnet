using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanViewSettings) return Forbid();
        // Load or bootstrap default statuses
        var list = await _statusRepo.ListAsync(Workspace.Id);
        if (list.Count == 0)
        {
            var defaults = new[]
            {
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = Workspace.Id, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = Workspace.Id, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new Tickflo.Core.Entities.TicketStatus { WorkspaceId = Workspace.Id, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true },
            };
            foreach (var s in defaults)
            {
                await _statusRepo.CreateAsync(s);
            }
            list = await _statusRepo.ListAsync(Workspace.Id);
        }
        Statuses = list;

        // Load or bootstrap default priorities
        var plist = await _priorityRepo.ListAsync(Workspace.Id);
        if (plist.Count == 0)
        {
            var pdefs = new[]
            {
                new Tickflo.Core.Entities.TicketPriority { WorkspaceId = Workspace.Id, Name = "Low", Color = "warning", SortOrder = 1 },
                new Tickflo.Core.Entities.TicketPriority { WorkspaceId = Workspace.Id, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new Tickflo.Core.Entities.TicketPriority { WorkspaceId = Workspace.Id, Name = "High", Color = "error", SortOrder = 3 },
            };
            foreach (var p in pdefs) { await _priorityRepo.CreateAsync(p); }
            plist = await _priorityRepo.ListAsync(Workspace.Id);
        }
        Priorities = plist;
        // Load or bootstrap default types
        var tlist = await _typeRepo.ListAsync(Workspace.Id);
        if (tlist.Count == 0)
        {
            var tdefs = new[]
            {
                new Tickflo.Core.Entities.TicketType { WorkspaceId = Workspace.Id, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new Tickflo.Core.Entities.TicketType { WorkspaceId = Workspace.Id, Name = "Bug", Color = "error", SortOrder = 2 },
                new Tickflo.Core.Entities.TicketType { WorkspaceId = Workspace.Id, Name = "Feature", Color = "primary", SortOrder = 3 },
            };
            foreach (var t in tdefs) { await _typeRepo.CreateAsync(t); }
            tlist = await _typeRepo.ListAsync(Workspace.Id);
        }
        Types = tlist;
        
        // Load notification integration settings from workspace metadata or use defaults
        // For now, using defaults - these would typically be stored in a workspace_settings table
        NotificationsEnabled = true;
        
        // Email Integration - SMTP is default, always available
        EmailIntegrationEnabled = true;
        EmailProvider = "smtp";
        
        // SMS Integration - Disabled by default, requires setup
        SmsIntegrationEnabled = false;
        SmsProvider = "none";
        
        // Push Integration - Disabled by default, requires setup
        PushIntegrationEnabled = false;
        PushProvider = "none";
        
        // In-App - Always enabled, no external provider needed
        InAppNotificationsEnabled = true;
        
        BatchNotificationDelay = 30;
        DailySummaryHour = 9;
        MentionNotificationsUrgent = true;
        TicketAssignmentNotificationsHigh = true;
        
        return Page();
    }

    public async Task<IActionResult> OnPostAddStatusAsync(string slug)
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
            }
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddPriorityAsync(string slug)
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
            }
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostAddTypeAsync(string slug)
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
            }
        }
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(string slug, int id, string name, string color, int sortOrder)
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
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdatePriorityAsync(string slug, int id, string name, string color, int sortOrder)
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
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostUpdateTypeAsync(string slug, int id, string name, string color, int sortOrder)
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
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteStatusAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        await _statusRepo.DeleteAsync(Workspace.Id, id);
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeletePriorityAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        await _priorityRepo.DeleteAsync(Workspace.Id, id);
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var (uid, isAdmin) = await ResolveUserAsync();
        if (uid == 0) return Forbid();
        await EnsurePermissionsAsync(uid, isAdmin);
        if (!CanEditSettings) return Forbid();
        await _typeRepo.DeleteAsync(Workspace.Id, id);
        return RedirectToPage("/Workspaces/Settings", new { slug });
    }

    public async Task<IActionResult> OnPostSaveNotificationSettingsAsync(string slug)
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
}
