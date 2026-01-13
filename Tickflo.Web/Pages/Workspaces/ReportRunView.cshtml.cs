using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunViewModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceReportRunViewService _runViewService;

    public ReportRunViewModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunViewService runViewService)
    {
        _workspaceRepo = workspaceRepo;
        _runViewService = runViewService;
    }

    [BindProperty(SupportsGet = true)]
    public string WorkspaceSlug { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int RunId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Take { get; set; } = 500; // default display limit
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1; // 1-based page index, hides PageModel.Page()

    public Core.Entities.Workspace? Workspace { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }

    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();

    public int DisplayLimit { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId, int? take, int? page)
    {
        WorkspaceSlug = slug;
        ReportId = reportId;
        RunId = runId;
        if (take.HasValue && take.Value > 0) Take = Math.Min(take.Value, 5000);
        if (page.HasValue && page.Value > 0) Page = page.Value;
        DisplayLimit = Take;

        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        Workspace = ws;
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _runViewService.BuildAsync(ws.Id, uid, reportId, runId, Page, Take);
        if (EnsurePermissionOrForbid(data.CanViewReports) is IActionResult permCheck) return permCheck;
        if (data.Report == null || data.Run == null || data.PageData == null) return NotFound();
        Report = data.Report;
        Run = data.Run;
        var pageResult = data.PageData;
        Page = pageResult.Page;
        Take = pageResult.Take;
        DisplayLimit = pageResult.Take;
        TotalRows = pageResult.TotalRows;
        TotalPages = pageResult.TotalPages;
        FromRow = pageResult.FromRow;
        ToRow = pageResult.ToRow;
        HasContent = pageResult.HasContent;
        Headers = pageResult.Headers.ToList();
        Rows = pageResult.Rows.Select(r => r.ToList()).ToList();
        return Page();
    }
}
