namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Views;

[Authorize]
public class ReportRunViewModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunViewService runViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IWorkspaceReportRunViewService _runViewService = runViewService;

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

    public Workspace? Workspace { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }

    public List<string> Headers { get; set; } = [];
    public List<List<string>> Rows { get; set; } = [];

    public int DisplayLimit { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId, int? take, int? page)
    {
        this.WorkspaceSlug = slug;
        this.ReportId = reportId;
        this.RunId = runId;
        if (take.HasValue && take.Value > 0)
        {
            this.Take = Math.Min(take.Value, 5000);
        }

        if (page.HasValue && page.Value > 0)
        {
            this.Page = page.Value;
        }

        this.DisplayLimit = this.Take;

        var ws = await this._workspaceRepo.FindBySlugAsync(slug);
        if (ws == null)
        {
            return this.NotFound();
        }

        this.Workspace = ws;
        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this._runViewService.BuildAsync(ws.Id, uid, reportId, runId, this.Page, this.Take);
        if (this.EnsurePermissionOrForbid(data.CanViewReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (data.Report == null || data.Run == null || data.PageData == null)
        {
            return this.NotFound();
        }

        this.Report = data.Report;
        this.Run = data.Run;
        var pageResult = data.PageData;
        this.Page = pageResult.Page;
        this.Take = pageResult.Take;
        this.DisplayLimit = pageResult.Take;
        this.TotalRows = pageResult.TotalRows;
        this.TotalPages = pageResult.TotalPages;
        this.FromRow = pageResult.FromRow;
        this.ToRow = pageResult.ToRow;
        this.HasContent = pageResult.HasContent;
        this.Headers = [.. pageResult.Headers];
        this.Rows = [.. pageResult.Rows.Select(r => r.ToList())];
        return this.Page();
    }
}

