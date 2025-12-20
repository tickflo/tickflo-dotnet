using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Web.Services;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IReportingService _reportingService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int ReportId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public bool Ready { get; set; }

    public ReportsEditModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IHttpContextAccessor httpContextAccessor, IRolePermissionRepository rolePerms, IReportingService reportingService)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _httpContextAccessor = httpContextAccessor;
        _rolePerms = rolePerms;
        _reportingService = reportingService;
    }
    public bool CanViewReports { get; private set; }
    public bool CanEditReports { get; private set; }
    public bool CanCreateReports { get; private set; }

    // Definition inputs
    [BindProperty]
    public string Source { get; set; } = "tickets";
    [BindProperty]
    public string FieldsCsv { get; set; } = string.Empty; // comma-separated
    [BindProperty]
    public string? FiltersJson { get; set; }

    // Schedule inputs
    [BindProperty]
    public bool ScheduleEnabled { get; set; }
    [BindProperty]
    public string ScheduleType { get; set; } = "none"; // none|daily|weekly|monthly
    [BindProperty]
    public string? ScheduleTime { get; set; } // HH:mm
    [BindProperty]
    public int? ScheduleDayOfWeek { get; set; }
    [BindProperty]
    public int? ScheduleDayOfMonth { get; set; }

    public IReadOnlyDictionary<string, string[]> Sources => _reportingService.GetAvailableSources();

    public async Task<IActionResult> OnGetAsync(string slug, int reportId = 0)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, uid);
        if (isAdmin)
        {
            CanViewReports = CanEditReports = CanCreateReports = true;
        }
        else if (eff.TryGetValue("reports", out var rp))
        {
            CanViewReports = rp.CanView;
            CanEditReports = rp.CanEdit;
            CanCreateReports = rp.CanCreate;
        }
        if (!CanViewReports) return Forbid();

        if (reportId > 0)
        {
            var rep = await _reportRepo.FindAsync(workspaceId, reportId);
            if (rep == null) return NotFound();
            ReportId = rep.Id;
            Name = rep.Name;
            Ready = rep.Ready;
            // Parse definition
            var def = ParseDefinition(rep.DefinitionJson);
            Source = def.source ?? "tickets";
            FieldsCsv = string.Join(",", def.fields ?? Array.Empty<string>());
            FiltersJson = def.filtersJson;
            ScheduleEnabled = rep.ScheduleEnabled;
            ScheduleType = rep.ScheduleType ?? "none";
            ScheduleTime = rep.ScheduleTime.HasValue ? new DateTime(rep.ScheduleTime.Value.Ticks).ToString("HH:mm") : null;
            ScheduleDayOfWeek = rep.ScheduleDayOfWeek;
            ScheduleDayOfMonth = rep.ScheduleDayOfMonth;
        }
        else
        {
            ReportId = 0;
            Name = string.Empty;
            Ready = false;
            Source = "tickets";
            FieldsCsv = "Id,Subject,Status,CreatedAt";
            FiltersJson = "";
            ScheduleEnabled = false;
            ScheduleType = "none";
            ScheduleTime = null;
            ScheduleDayOfWeek = null;
            ScheduleDayOfMonth = null;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, uid);
        bool allowed = isAdmin;
        if (!allowed && eff.TryGetValue("reports", out var rp))
        {
            allowed = (ReportId == 0) ? rp.CanCreate : rp.CanEdit;
        }
        if (!allowed) return Forbid();
        if (!ModelState.IsValid) return Page();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var defJson = BuildDefinitionJson(Source, FieldsCsv, FiltersJson);
        TimeSpan? schedTime = null;
        if (!string.IsNullOrWhiteSpace(ScheduleTime) && TimeSpan.TryParse(ScheduleTime, out var ts)) schedTime = ts;

        if (ReportId == 0)
        {
            await _reportRepo.CreateAsync(new Report { WorkspaceId = workspaceId, Name = nameTrim, Ready = Ready,
                DefinitionJson = defJson, ScheduleEnabled = ScheduleEnabled, ScheduleType = ScheduleType, ScheduleTime = schedTime,
                ScheduleDayOfWeek = ScheduleDayOfWeek, ScheduleDayOfMonth = ScheduleDayOfMonth });
            TempData["Success"] = $"Report '{Name}' created successfully.";
        }
        else
        {
            var updated = await _reportRepo.UpdateAsync(new Report { Id = ReportId, WorkspaceId = workspaceId, Name = nameTrim, Ready = Ready,
                DefinitionJson = defJson, ScheduleEnabled = ScheduleEnabled, ScheduleType = ScheduleType, ScheduleTime = schedTime,
                ScheduleDayOfWeek = ScheduleDayOfWeek, ScheduleDayOfMonth = ScheduleDayOfMonth });
            if (updated == null) return NotFound();
            TempData["Success"] = $"Report '{Name}' updated successfully.";
        }
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Reports", new { slug, Query = queryQ, PageNumber = pageQ });
    }

    private (string source, string[] fields, string? filtersJson) ParseDefinition(string? json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return ("tickets", new []{"Id","Subject","Status","CreatedAt"}, null);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var src = root.TryGetProperty("source", out var s) ? s.GetString() ?? "tickets" : "tickets";
            string[] fields = Array.Empty<string>();
            if (root.TryGetProperty("fields", out var f) && f.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                fields = f.EnumerateArray().Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String).Select(e => e.GetString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }
            string? filtersJson = null;
            if (root.TryGetProperty("filters", out var fl))
            {
                filtersJson = fl.GetRawText();
            }
            return (src, fields, filtersJson);
        }
        catch { return ("tickets", new []{"Id","Subject","Status","CreatedAt"}, null); }
    }

    private string BuildDefinitionJson(string source, string fieldsCsv, string? filtersJson)
    {
        var fields = (fieldsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
        var filtersPart = string.IsNullOrWhiteSpace(filtersJson) ? "\"filters\":[]" : $"\"filters\":{filtersJson}";
        var json = $"{{\"source\":\"{(source ?? "tickets").ToLowerInvariant()}\",\"fields\":[{string.Join(',', fields.Select(f => $"\"{f}\""))}],{filtersPart}}}";
        return json;
    }
}
