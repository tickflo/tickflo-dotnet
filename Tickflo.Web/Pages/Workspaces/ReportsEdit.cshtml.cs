using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportCommandService _reportCommandService;
    private readonly IReportDefinitionValidator _defValidator;
    private readonly IWorkspaceReportsEditViewService _reportsEditViewService;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int ReportId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public bool Ready { get; set; }

    public ReportsEditModel(IWorkspaceRepository workspaceRepo, IReportCommandService reportCommandService, IReportDefinitionValidator defValidator, IWorkspaceReportsEditViewService reportsEditViewService)
    {
        _workspaceRepo = workspaceRepo;
        _reportCommandService = reportCommandService;
        _defValidator = defValidator;
        _reportsEditViewService = reportsEditViewService;
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

    public IReadOnlyDictionary<string, string[]> Sources { get; private set; } = new Dictionary<string, string[]>();

    public async Task<IActionResult> OnGetAsync(string slug, int reportId = 0)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var data = await _reportsEditViewService.BuildAsync(workspaceId, uid, reportId);
        CanViewReports = data.CanViewReports;
        CanEditReports = data.CanEditReports;
        CanCreateReports = data.CanCreateReports;
        Sources = data.Sources;
        if (!CanViewReports) return Forbid();

        if (reportId > 0)
        {
            var rep = data.ExistingReport;
            if (rep == null) return NotFound();
            ReportId = rep.Id;
            Name = rep.Name;
            Ready = rep.Ready;
            // Parse definition
            var def = _defValidator.Parse(rep.DefinitionJson);
            Source = def.Source;
            FieldsCsv = string.Join(",", def.Fields);
            FiltersJson = def.FiltersJson;
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
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var data = await _reportsEditViewService.BuildAsync(workspaceId, uid, ReportId);
        bool allowed = (ReportId == 0) ? data.CanCreateReports : data.CanEditReports;
        if (!allowed) return Forbid();
        if (!ModelState.IsValid) return Page();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var defJson = _defValidator.BuildJson(Source, FieldsCsv, FiltersJson);
        TimeSpan? schedTime = null;
        if (!string.IsNullOrWhiteSpace(ScheduleTime) && TimeSpan.TryParse(ScheduleTime, out var ts)) schedTime = ts;

        if (ReportId == 0)
        {
            await _reportCommandService.CreateAsync(new Report { WorkspaceId = workspaceId, Name = nameTrim, Ready = Ready,
                DefinitionJson = defJson, ScheduleEnabled = ScheduleEnabled, ScheduleType = ScheduleType, ScheduleTime = schedTime,
                ScheduleDayOfWeek = ScheduleDayOfWeek, ScheduleDayOfMonth = ScheduleDayOfMonth });
            TempData["Success"] = $"Report '{Name}' created successfully.";
        }
        else
        {
            var updated = await _reportCommandService.UpdateAsync(new Report { Id = ReportId, WorkspaceId = workspaceId, Name = nameTrim, Ready = Ready,
                DefinitionJson = defJson, ScheduleEnabled = ScheduleEnabled, ScheduleType = ScheduleType, ScheduleTime = schedTime,
                ScheduleDayOfWeek = ScheduleDayOfWeek, ScheduleDayOfMonth = ScheduleDayOfMonth });
            if (updated == null) return NotFound();
            TempData["Success"] = $"Report '{Name}' updated successfully.";
        }
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Reports", new { slug, Query = queryQ, PageNumber = pageQ });
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
}
